using System.Text.Json.Serialization;
using Azure.Identity;
using FirebaseAdmin;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.SqlServer;
using JobFlow.API.Constants;
using JobFlow.API.Hubs;
using JobFlow.API.Mappings;
using JobFlow.Business.ConfigurationSettings;
using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Validators;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.Extensions;
using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using JobFlow.Infrastructure.ExternalServices.ConfigurationModels;
using JobFlow.Infrastructure.HttpClients;
using JobFlow.Infrastructure.Middleware;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF;
using QuestPDF.Infrastructure;
using Stripe;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// ============================================================
// CONFIGURATION SOURCES (Hybrid: Appsettings + UserSecrets + KeyVault + EnvVars)
// ============================================================

builder.Configuration
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables();

if (env.IsDevelopment())
{
    // Add User Secrets for local dev, safe and offline
    builder.Configuration.AddUserSecrets<Program>(true);
}

// Add Key Vault if configured
var keyVaultUri = builder.Configuration[ConfigConstants.KEY_VAULT_URI];
if (!env.IsDevelopment() && !string.IsNullOrWhiteSpace(keyVaultUri))
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());


// ============================================================
// BIND SETTINGS CLASSES
// ============================================================

builder.Services.Configure<FrontEndSettings>(builder.Configuration.GetSection("Frontend"));
builder.Services.AddSingleton<IFrontendSettings>(sp => sp.GetRequiredService<IOptions<FrontEndSettings>>().Value);

builder.Services.Configure<BackendSettings>(builder.Configuration.GetSection("Backend"));
builder.Services.AddSingleton<IBackendSettings>(sp => sp.GetRequiredService<IOptions<BackendSettings>>().Value);

builder.Services.Configure<PaymentSettings>(builder.Configuration.GetSection("Payments"));
builder.Services.AddSingleton<IPaymentSettings>(sp => sp.GetRequiredService<IOptions<PaymentSettings>>().Value);

// ============================================================
// FIREBASE INITIALIZATION
// ============================================================

var firebaseFilePath = Path.Combine(env.ContentRootPath, "job-flow-firebase-adminsdk.json");

if (!System.IO.File.Exists(firebaseFilePath))
    throw new InvalidOperationException($"Firebase service account file not found: {firebaseFilePath}");

string firebaseProjectId;
using (var doc = JsonDocument.Parse(System.IO.File.ReadAllText(firebaseFilePath)))
{
    firebaseProjectId = doc.RootElement.GetProperty("project_id").GetString() ?? "";
}

if (string.IsNullOrWhiteSpace(firebaseProjectId))
    throw new InvalidOperationException("Firebase project_id is missing in job-flow-firebase-adminsdk.json");

// Create the Firebase Admin default app instance so FirebaseAuth.DefaultInstance is available.
if (FirebaseApp.DefaultInstance is null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseFilePath)
    });
}

builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// MVC / CONTROLLERS
// ============================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<JsonOptions>(options =>
{
	options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// ============================================================
// SIGNALR
// ============================================================

builder.Services.AddSignalR();

// ============================================================
// DATABASE
// ============================================================

string? appConnectionString;

if (env.IsDevelopment())
{
    appConnectionString = builder.Configuration.GetConnectionString("JobFlowDB");
    if (string.IsNullOrWhiteSpace(appConnectionString))
        throw new InvalidOperationException(
            "JobFlowDB connection string is missing in appsettings.Development.json or User Secrets.");
}
else
{
    appConnectionString = builder.Configuration[ConfigConstants.APP_CONNECTIONSTRING_NAME];
    if (string.IsNullOrWhiteSpace(appConnectionString))
        throw new InvalidOperationException(
            $"Missing Key Vault connection string: {ConfigConstants.APP_CONNECTIONSTRING_NAME}");
}

builder.Services.AddDbContextFactory<JobFlowDbContext>(options => options.UseSqlServer(appConnectionString, b =>
{
    b.MigrationsAssembly("JobFlow.Infrastructure.Persistence");
    b.CommandTimeout(170);
    b.EnableRetryOnFailure(8, TimeSpan.FromSeconds(10), null);
}));

// ============================================================
// HANGFIRE CONFIGURATION
// ============================================================

builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(appConnectionString, new SqlServerStorageOptions
    {
        SchemaName = ConfigConstants.HANGFIRE_SCHEMA_NAME,
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

// ============================================================
// SWAGGER / OPENAPI
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "JobFlow API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token."
    });
    c.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// ============================================================
// CORS
// ============================================================

var apiAllowOrigins = "JobFlowAPIAllowOrigins";
builder.Services.AddCors(o =>
{
    o.AddPolicy(apiAllowOrigins, p => p
        .SetIsOriginAllowed(origin =>
        {
            var host = new Uri(origin).Host;
            return host == "localhost"
                   || host == "gojobflow.com"
                   || host == "www.gojobflow.com"
                   || host.EndsWith(".gojobflow.app")
                   || host.EndsWith(".gojobflow.com");
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ============================================================
// THIRD-PARTY SETTINGS (Stripe, Twilio, Brevo, ReCAPTCHA, Square)
// ============================================================

builder.Services.Configure<StripeSettings>(options =>
{
    options.ApiKey = builder.Configuration["StripeSettings-ApiKey"] ?? "";
    options.ReturnUrl = builder.Configuration["StripeSettings-ReturnUrl"] ?? "";
    options.RefreshUrl = builder.Configuration["StripeSettings-RefreshUrl"] ?? "";
    options.WebhookKey = builder.Configuration["StripeSettings-WebhookKey"] ?? "";
});

builder.Services.Configure<TwilioSettings>(options =>
{
    options.SenderPhoneNumber = builder.Configuration["Twilio-SenderPhoneNumber"] ?? "";
    options.AccountSId = builder.Configuration["Twilio-AccountSId"] ?? "";
    options.AuthToken = builder.Configuration["Twilio-AuthToken"] ?? "";
    options.MessagingServiceSid = builder.Configuration["Twilio-MessagingServiceSid"] ?? "";
});

builder.Services.Configure<BrevoSettings>(options =>
{
    options.ApiKey = builder.Configuration["BrevoSettings-ApiKey"] ?? "";
});

builder.Services.Configure<ReCAPTCHASettings>(options =>
{
    options.SecretKey = builder.Configuration["reCAPTCHA-Api"] ?? "";
});

builder.Services.Configure<SquareSettings>(options =>
{
    options.ApplicationId = builder.Configuration["SquareSettings-ApplicationId"];
    options.AccessToken = builder.Configuration["SquareSettings-AccessToken"];
    options.LocationId = builder.Configuration["SquareSettings-LocationId"];
});

builder.Services.AddSingleton<ITwilioSettings>(sp => sp.GetRequiredService<IOptions<TwilioSettings>>().Value);
builder.Services.AddSingleton<IStripeSettings>(sp => sp.GetRequiredService<IOptions<StripeSettings>>().Value);
builder.Services.AddSingleton<IBrevoSettings>(sp => sp.GetRequiredService<IOptions<BrevoSettings>>().Value);
builder.Services.AddSingleton<IReCAPTCHASettings>(sp => sp.GetRequiredService<IOptions<ReCAPTCHASettings>>().Value);
builder.Services.AddSingleton<ISquareSettings>(sp => sp.GetRequiredService<IOptions<SquareSettings>>().Value);

// ============================================================
// DEPENDENCY INJECTION, MAPPINGS, AUTHORIZATION
// ============================================================

builder.Services.AddMapsterConfiguration();
builder.Services.AddScoped<IUnitOfWork, JobFlowUnitOfWork>();
builder.Services.AddJobFlowHttpClients();
builder.Services.AddAttributedServices(typeof(IJobFlowHttpClientFactory).Assembly, typeof(IUserService).Assembly);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganizationAdminOnly", policy => policy.RequireRole(UserRoles.OrganizationAdmin));
    options.AddPolicy("OrganizationEmployeeOnly", policy => policy.RequireRole(UserRoles.OrganizationEmployee));
    options.AddPolicy("OrganizationClientOnly", policy => policy.RequireRole(UserRoles.OrganizationClient));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole(UserRoles.SuperAdmin));
    options.AddPolicy("KatharixAdminOnly", policy => policy.RequireRole(UserRoles.KatharixAdmin));
    options.AddPolicy("KatharixEmployeeOnly", policy => policy.RequireRole(UserRoles.KatharixEmployee));
});

// ============================================================
// LOGGING, MIDDLEWARE, HANGFIRE DASHBOARD
// ============================================================

builder.Services.AddHttpContextAccessor();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
Settings.License = LicenseType.Community;

if (env.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o =>
    {
        o.ListenLocalhost(44398, lo =>
        {
            lo.UseHttps();
            lo.Protocols = HttpProtocols.Http1;
        });
        o.ListenLocalhost(5099, lo => { lo.Protocols = HttpProtocols.Http1; });
    });
}

// ============================================================
// BUILD AND PIPELINE
// ============================================================

var app = builder.Build();

StripeConfiguration.ApiKey = builder.Configuration["StripeSettings-ApiKey"];

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler(app.Environment.IsProduction() ? "/error" : "/error-development");

app.UseRouting();
app.UseCors(apiAllowOrigins);
if (app.Environment.IsDevelopment()) app.UseHangfireDashboard();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStatusCodePages();
app.UseAuthentication();

app.UseMiddleware<FirebaseAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();