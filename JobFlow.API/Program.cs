using Azure.Identity;
using FirebaseAdmin;
using FluentValidation;
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
using JobFlow.Infrastructure.PaymentGateways.Stripe;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

builder.Configuration
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.Configure<FrontEndSettings>(
    builder.Configuration.GetSection("Frontend"));

builder.Services.AddSingleton<IFrontendSettings>(sp =>
    sp.GetRequiredService<IOptions<FrontEndSettings>>().Value);

builder.Services.Configure<BackendSettings>(
    builder.Configuration.GetSection("Backend"));

builder.Services.AddSingleton<IBackendSettings>(sp =>
    sp.GetRequiredService<IOptions<BackendSettings>>().Value);

var tempConfig = new ConfigurationBuilder()
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var keyVaultUri = tempConfig[ConfigConstants.KEY_VAULT_URI];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var firebaseJson = builder.Configuration[ConfigConstants.FIREBASE_ADMIN_SDK];
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromJson(firebaseJson)
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddValidatorsFromAssemblyContaining<OrganizationValidator>();

builder.Services.AddControllers()
       .AddJsonOptions(options =>
       {
           options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
       })
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = false);

builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddAuthentication();

string? appConnectionString;

if (env.IsDevelopment())
{
    // Use local DB during development
    appConnectionString = builder.Configuration.GetConnectionString("JobFlowDB");
    if (string.IsNullOrWhiteSpace(appConnectionString))
        throw new InvalidOperationException("JobFlowDB connection string is missing in appsettings.Development.json.");
}
else
{
    // Use secure connection string from Key Vault or environment
    appConnectionString = builder.Configuration[ConfigConstants.APP_CONNECTIONSTRING_NAME];
    if (string.IsNullOrWhiteSpace(appConnectionString))
        throw new InvalidOperationException($"Missing Key Vault connection string: {ConfigConstants.APP_CONNECTIONSTRING_NAME}");
}

builder.Services.AddDbContextFactory<JobFlowDbContext>(options => options.UseSqlServer(appConnectionString,
    b =>
    {
        b.MigrationsAssembly("JobFlow.Infrastructure.Persistence");
        b.CommandTimeout(170);
        b.EnableRetryOnFailure(8, TimeSpan.FromSeconds(10), null);
    }
));

builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(appConnectionString, new SqlServerStorageOptions
    {
        SchemaName = ConfigConstants.HANGFIRE_SCHEMA_NAME ,
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    })
);

builder.Services.AddHangfireServer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobFlow API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

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
                || host.EndsWith(".gojobflow.com"); // app., i., etc. if needed
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});




builder.Services.Configure<StripeSettings>(options =>
{
    options.ApiKey = builder.Configuration[$"StripeSettings-ApiKey"] ?? "";
    options.ReturnUrl = builder.Configuration[$"StripeSettings-ReturnUrl"] ?? "";
    options.RefreshUrl = builder.Configuration[$"StripeSettings-RefreshUrl"] ?? "";
    options.WebhookKey = builder.Configuration[$"StripeSettings-WebhookKey"] ?? "";
});

builder.Services.Configure<TwilioSettings>(options =>
{
    options.SenderPhoneNumber = builder.Configuration[$"Twilio-SenderPhoneNumber"] ?? "";
    options.AccountSId = builder.Configuration[$"Twilio-AccountSId"] ?? "";
    options.AuthToken = builder.Configuration[$"Twilio-AuthToken"] ?? "";
    options.MessagingServiceSid = builder.Configuration[$"Twilio-MessagingServiceSid"] ?? "";
});

builder.Services.Configure<BrevoSettings>(options =>
{
    options.ApiKey = builder.Configuration[$"BrevoSettings-ApiKey"] ?? "";
});

builder.Services.Configure<ReCAPTCHASettings>(options =>
{
    options.SecretKey = builder.Configuration[$"reCAPTCHA-Api"] ?? "";
});

builder.Services.Configure<SquareSettings>(options =>
{
    options.ApplicationId = builder.Configuration[$"SquareSettings-ApplicationId"];
    options.AccessToken = builder.Configuration[$"SquareSettings-AccessToken"];
    options.LocationId = builder.Configuration[$"SquareSettings-LocationId"];
});

builder.Services.AddSingleton<ITwilioSettings>(sp => sp.GetRequiredService<IOptions<TwilioSettings>>().Value);
builder.Services.AddSingleton<IStripeSettings>(sp => sp.GetRequiredService<IOptions<StripeSettings>>().Value);
builder.Services.AddSingleton<IBrevoSettings>(sp => sp.GetRequiredService<IOptions<BrevoSettings>>().Value);
builder.Services.AddSingleton<IReCAPTCHASettings>(sp => sp.GetRequiredService<IOptions<ReCAPTCHASettings>>().Value);
builder.Services.AddSingleton<ISquareSettings>(sp => sp.GetRequiredService<IOptions<SquareSettings>>().Value);
builder.Services.AddMapsterConfiguration();


builder.Services.AddScoped<IUnitOfWork, JobFlowUnitOfWork>();

builder.Services.AddJobFlowHttpClients();
builder.Services.AddAttributedServices(
    typeof(IJobFlowHttpClientFactory).Assembly,
    typeof(IUserService).Assembly
);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganizationAdminOnly", policy => policy.RequireRole(UserRoles.OrganizationAdmin));
    options.AddPolicy("OrgaizationEmployeeOnly", policy => policy.RequireRole(UserRoles.OrganizationEmployee));
    options.AddPolicy("OrgaizationClientOnly", policy => policy.RequireRole(UserRoles.OrganizationClient));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole(UserRoles.SuperAdmin));
    options.AddPolicy("KatharixAdminOnly", policy => policy.RequireRole(UserRoles.KatharixAdmin));
    options.AddPolicy("KatharixEmployeeOnly", policy => policy.RequireRole(UserRoles.KatharixEmployee));
});

builder.Services.AddHttpContextAccessor();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o =>
    {
        // Match your ports from launchSettings.json
        o.ListenLocalhost(44398, lo => { lo.UseHttps(); lo.Protocols = HttpProtocols.Http1; });
        o.ListenLocalhost(5099, lo => { lo.Protocols = HttpProtocols.Http1; });
    });
}
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var test = scope.ServiceProvider.GetRequiredService<StripePaymentProcessor>();
    Console.WriteLine($"Resolved StripePaymentProcessor: {test.GetType().Name}");
}

StripeConfiguration.ApiKey = builder.Configuration[$"StripeSettings-ApiKey"];

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (!app.Environment.IsProduction())
{
    app.UseExceptionHandler("/error-development");
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseCors(apiAllowOrigins);
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
