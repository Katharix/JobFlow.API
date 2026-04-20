using System.Text.Json.Serialization;
using Azure.Identity;
using FirebaseAdmin;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.SqlServer;
using JobFlow.API.Constants;
using JobFlow.API.Filters;
using JobFlow.API.Hubs;
using JobFlow.API.Mappings;
using JobFlow.API.Services;
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
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using QuestPDF;
using QuestPDF.Infrastructure;
using Stripe;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// On Linux App Service, bind explicitly to the platform-provided port to avoid startup timeouts.
var appServicePort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(appServicePort))
    builder.WebHost.UseUrls($"http://0.0.0.0:{appServicePort}");

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
if (!string.IsNullOrWhiteSpace(keyVaultUri))
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

var firebaseAdminSdkJson = builder.Configuration[ConfigConstants.FIREBASE_ADMIN_SDK];
string firebaseProjectId;
string firebaseClientEmail;
GoogleCredential firebaseCredential;

if (!string.IsNullOrWhiteSpace(firebaseAdminSdkJson))
{
    var normalizedFirebaseAdminSdkJson = NormalizeFirebaseAdminSdkJson(firebaseAdminSdkJson);
    using var doc = ParseFirebaseAdminSdkJson(normalizedFirebaseAdminSdkJson);
    firebaseProjectId = doc.RootElement.GetProperty("project_id").GetString() ?? "";
    firebaseClientEmail = doc.RootElement.TryGetProperty("client_email", out var clientEmailElement)
        ? clientEmailElement.GetString() ?? ""
        : "";
    var credential = CredentialFactory.FromJson<ServiceAccountCredential>(normalizedFirebaseAdminSdkJson);
    firebaseCredential = credential.ToGoogleCredential();
}
else
{
    var firebaseFilePath = Path.Combine(env.ContentRootPath, "job-flow-firebase-adminsdk.json");

    if (!System.IO.File.Exists(firebaseFilePath))
        throw new InvalidOperationException(
            $"Firebase admin credentials were not found. Configure '{ConfigConstants.FIREBASE_ADMIN_SDK}' in Key Vault or provide local file: {firebaseFilePath}");

    var firebaseJson = System.IO.File.ReadAllText(firebaseFilePath);

    using var doc = JsonDocument.Parse(firebaseJson);
    firebaseProjectId = doc.RootElement.GetProperty("project_id").GetString() ?? "";
    firebaseClientEmail = doc.RootElement.TryGetProperty("client_email", out var clientEmailElement)
        ? clientEmailElement.GetString() ?? ""
        : "";
    var credential = CredentialFactory.FromFile<ServiceAccountCredential>(firebaseFilePath);
    firebaseCredential = credential.ToGoogleCredential();
}

if (string.IsNullOrWhiteSpace(firebaseProjectId))
    throw new InvalidOperationException("Firebase project_id is missing in configured Firebase admin credentials.");

static string NormalizeFirebaseAdminSdkJson(string rawJson)
{
    var json = rawJson.Trim();

    // App settings and vault entries are sometimes stored as a JSON string literal.
    // Example: "{\"type\":\"service_account\",...}"
    if (json.Length >= 2 && json[0] == '"' && json[^1] == '"')
    {
        try
        {
            var unescaped = JsonSerializer.Deserialize<string>(json);
            if (!string.IsNullOrWhiteSpace(unescaped))
                json = unescaped.Trim();
        }
        catch (JsonException)
        {
            // Keep original value; parser will provide the final actionable error.
        }
    }

    // Some deployments provide object JSON with escaped quotes but no outer quotes.
    // Example: {\"type\":\"service_account\",...}
    if (json.StartsWith("{\\\"") || json.Contains("\\\"project_id\\\""))
        json = json.Replace("\\\"", "\"");

    return json;
}

static JsonDocument ParseFirebaseAdminSdkJson(string json)
{
    try
    {
        return JsonDocument.Parse(json);
    }
    catch (JsonException ex)
    {
        var preview = json.Length > 80 ? json[..80] + "..." : json;
        throw new InvalidOperationException(
            $"Invalid Firebase admin SDK JSON in configuration key '{ConfigConstants.FIREBASE_ADMIN_SDK}'. Preview='{preview}'",
            ex);
    }
}

// Create the Firebase Admin default app instance so FirebaseAuth.DefaultInstance is available.
if (FirebaseApp.DefaultInstance is null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = firebaseCredential
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
    })
    .AddJwtBearer("ClientPortalJwt", options =>
    {
        var signingKey = builder.Configuration["Auth:ClientPortal:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
            signingKey = builder.Configuration["Auth-ClientPortal-SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException("Missing configuration: Auth:ClientPortal:SigningKey");

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken)
                    && (path.StartsWithSegments("/hubs/client-chat")
                        || path.StartsWithSegments("/hubs/client-portal")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "JobFlow.ClientPortal",
            ValidateAudience = true,
            ValidAudience = "JobFlow.ClientPortal",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// MVC / CONTROLLERS
// ============================================================

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidateModelStateFilter>();
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddValidatorsFromAssemblyContaining<OrganizationValidator>();

// ============================================================
// SIGNALR
// ============================================================

builder.Services.AddSignalR();
builder.Services.AddDataProtection();
builder.Services.AddDistributedMemoryCache();

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
    // Prefer standard connection-string binding so Key Vault secret
    // ConnectionStrings--JobFlowDB works out of the box.
    appConnectionString = builder.Configuration.GetConnectionString("JobFlowDB");

    // Support direct Key Vault secret name: JobFlowDB.
    if (string.IsNullOrWhiteSpace(appConnectionString))
        appConnectionString = builder.Configuration["JobFlowDB"];

    // Backward compatibility for existing Key Vault secret naming.
    if (string.IsNullOrWhiteSpace(appConnectionString))
        appConnectionString = builder.Configuration[ConfigConstants.APP_CONNECTIONSTRING_NAME];

    if (string.IsNullOrWhiteSpace(appConnectionString))
        throw new InvalidOperationException(
            $"Missing DB connection string. Configure Key Vault secret 'JobFlowDB', 'ConnectionStrings--JobFlowDB', or '{ConfigConstants.APP_CONNECTIONSTRING_NAME}'.");
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
var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

foreach (var origin in configuredOrigins)
{
    if (!string.IsNullOrWhiteSpace(origin))
        allowedOrigins.Add(origin.TrimEnd('/'));
}

var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"];
if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
    allowedOrigins.Add(frontendBaseUrl.TrimEnd('/'));

if (env.IsDevelopment())
{
    allowedOrigins.Add("https://localhost:44398");
    allowedOrigins.Add("http://localhost:5099");
    allowedOrigins.Add("http://localhost:4200");
}

builder.Services.AddCors(o =>
{
    o.AddPolicy(apiAllowOrigins, p => p
        .SetIsOriginAllowed(origin =>
        {
            var host = new Uri(origin).Host;
            return host == "localhost"
                   || host == "gojobflow.com"
                   || host == "www.gojobflow.com"
                                     || host == "jobflow-ui-web-staging.web.app"
                                     || host == "jobflow-ui-web-staging.firebaseapp.com"
                                     || host == "jobflow-api-staging.azurewebsites.net"
                                     || host == "staging.gojobflow.com"
                                     || host == "api.staging.gojobflow.com"
                   || host.EndsWith(".gojobflow.app")
                   || host.EndsWith(".gojobflow.com");
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("payment-sensitive", context =>
    {
        var userKey = context.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userKey))
            userKey = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(userKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 40,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("webhook", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 80,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
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
    options.GoMonthlyPrice = builder.Configuration["StripeSettings-GoMonthlyPrice"] ?? "";
    options.GoYearlyPrice = builder.Configuration["StripeSettings-GoYearlyPrice"] ?? "";
    options.FlowMonthlyPrice = builder.Configuration["StripeSettings-FlowMonthlyPrice"] ?? "";
    options.FlowYearlyPrice = builder.Configuration["StripeSettings-FlowYearlyPrice"] ?? "";
    options.MaxMonthlyPrice = builder.Configuration["StripeSettings-MaxMonthlyPrice"] ?? "";
    options.MaxYearlyPrice = builder.Configuration["StripeSettings-MaxYearlyPrice"] ?? "";
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

builder.Services.Configure<JobFlow.Infrastructure.ExternalServices.Turnstile.TurnstileOptions>(options =>
{
    options.SecretKey = builder.Configuration["Turnstile-SecretKey"] ?? "";
    options.ExpectedHostname = builder.Configuration["Turnstile-ExpectedHostname"] ?? "";
});

builder.Services.AddHttpClient<
    JobFlow.Infrastructure.ExternalServices.Turnstile.ICaptchaVerificationService,
    JobFlow.Infrastructure.ExternalServices.Turnstile.TurnstileVerificationService>();

builder.Services.Configure<SquareSettings>(options =>
{
    var squareSection = builder.Configuration.GetSection("SquareSettings");

    options.ApplicationId = squareSection["ApplicationId"] ?? builder.Configuration["SquareSettings-ApplicationId"];
    options.ClientSecret = squareSection["ClientSecret"] ?? builder.Configuration["SquareSettings-ClientSecret"];
    options.AccessToken = squareSection["AccessToken"] ?? builder.Configuration["SquareSettings-AccessToken"];
    options.LocationId = squareSection["LocationId"] ?? builder.Configuration["SquareSettings-LocationId"];
    options.RedirectUrl = squareSection["RedirectUrl"] ?? builder.Configuration["SquareSettings-RedirectUrl"];
    options.WebhookSignatureKey = squareSection["WebhookSignatureKey"] ?? builder.Configuration["SquareSettings-WebhookSignatureKey"];
    options.WebhookNotificationUrl = squareSection["WebhookNotificationUrl"] ?? builder.Configuration["SquareSettings-WebhookNotificationUrl"];
    options.UseSandbox = bool.TryParse(squareSection["UseSandbox"] ?? builder.Configuration["SquareSettings-UseSandbox"], out var useSandbox) && useSandbox;
});

builder.Services.AddSingleton<ITwilioSettings>(sp => sp.GetRequiredService<IOptions<TwilioSettings>>().Value);
builder.Services.AddSingleton<IStripeSettings>(sp => sp.GetRequiredService<IOptions<StripeSettings>>().Value);
builder.Services.AddSingleton<IBrevoSettings>(sp => sp.GetRequiredService<IOptions<BrevoSettings>>().Value);
builder.Services.AddSingleton<ISquareSettings>(sp => sp.GetRequiredService<IOptions<SquareSettings>>().Value);

// ============================================================
// DEPENDENCY INJECTION, MAPPINGS, AUTHORIZATION
// ============================================================

builder.Services.AddMapsterConfiguration();
builder.Services.AddScoped<IUnitOfWork, JobFlowUnitOfWork>();
builder.Services.AddSingleton<IWebHostEnvironmentAccessor, WebHostEnvironmentAccessor>();
builder.Services.AddScoped<IInvoiceRealtimeNotifier, InvoiceRealtimeNotifier>();
builder.Services.AddScoped<IOrganizationRealtimeNotifier, OrganizationRealtimeNotifier>();
builder.Services.AddScoped<IFollowUpJobScheduler, FollowUpJobScheduler>();
builder.Services.AddScoped<ClientImportCsvService>();
builder.Services.AddScoped<ClientImportProcessor>();
builder.Services.AddScoped<ClientImportUploadSessionService>();
builder.Services.AddScoped<EmployeeImportCsvService>();
builder.Services.AddScoped<EmployeeImportProcessor>();
builder.Services.AddScoped<EmployeeImportUploadSessionService>();
builder.Services.AddScoped<DataExportBuilderService>();
builder.Services.AddScoped<DataExportJobProcessor>();
builder.Services.AddScoped<IEstimateRevisionNotificationJob, EstimateRevisionNotificationJob>();
builder.Services.AddJobFlowHttpClients();
builder.Services.AddAttributedServices(typeof(IJobFlowHttpClientFactory).Assembly, typeof(IUserService).Assembly);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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

// ============================================================
// BUILD AND PIPELINE
// ============================================================

var app = builder.Build();

var hasConnectionStringsJobFlowDb = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("JobFlowDB"));
var hasDirectJobFlowDb = !string.IsNullOrWhiteSpace(builder.Configuration["JobFlowDB"]);
var hasLegacySqlConnectionString = !string.IsNullOrWhiteSpace(builder.Configuration[ConfigConstants.APP_CONNECTIONSTRING_NAME]);
var hasFirebaseAdminSdk = !string.IsNullOrWhiteSpace(builder.Configuration[ConfigConstants.FIREBASE_ADMIN_SDK]);
var hasClientPortalSigningKeyColon = !string.IsNullOrWhiteSpace(builder.Configuration["Auth:ClientPortal:SigningKey"]);
var hasClientPortalSigningKeyHyphen = !string.IsNullOrWhiteSpace(builder.Configuration["Auth-ClientPortal-SigningKey"]);
var hasStripeApiKey = !string.IsNullOrWhiteSpace(builder.Configuration["StripeSettings-ApiKey"]);
var hasStripeReturnUrl = !string.IsNullOrWhiteSpace(builder.Configuration["StripeSettings-ReturnUrl"]);
var hasStripeWebhookKey = !string.IsNullOrWhiteSpace(builder.Configuration["StripeSettings-WebhookKey"]);

app.Logger.LogInformation(
    "Startup config check (sanitized): Env={Environment}, KeyVaultUriConfigured={KeyVaultUriConfigured}, DbConnectionStringsJobFlowDb={DbConnectionStringsJobFlowDb}, DbJobFlowDb={DbJobFlowDb}, DbSqlConnectionString={DbSqlConnectionString}, FirebaseAdminSdk={FirebaseAdminSdk}, FirebaseProjectId={FirebaseProjectId}, FirebaseClientEmail={FirebaseClientEmail}, ClientPortalSigningKeyColon={ClientPortalSigningKeyColon}, ClientPortalSigningKeyHyphen={ClientPortalSigningKeyHyphen}, StripeApiKeyConfigured={StripeApiKeyConfigured}, StripeReturnUrlConfigured={StripeReturnUrlConfigured}, StripeWebhookKeyConfigured={StripeWebhookKeyConfigured}, FrontendBaseUrlConfigured={FrontendBaseUrlConfigured}",
    env.EnvironmentName,
    !string.IsNullOrWhiteSpace(keyVaultUri),
    hasConnectionStringsJobFlowDb,
    hasDirectJobFlowDb,
    hasLegacySqlConnectionString,
    hasFirebaseAdminSdk,
    firebaseProjectId,
    firebaseClientEmail,
    hasClientPortalSigningKeyColon,
    hasClientPortalSigningKeyHyphen,
    hasStripeApiKey,
    hasStripeReturnUrl,
    hasStripeWebhookKey,
    !string.IsNullOrWhiteSpace(frontendBaseUrl));

using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobFlowDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

StripeConfiguration.ApiKey = builder.Configuration["StripeSettings-ApiKey"];

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseExceptionHandler(app.Environment.IsProduction() ? "/error" : "/error-development");
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRouting();
app.UseCors(apiAllowOrigins);
app.UseRateLimiter();
if (app.Environment.IsDevelopment()) app.UseHangfireDashboard();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStatusCodePages();
app.UseAuthentication();

app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<ClientChatHub>("/hubs/client-chat");
app.MapHub<NotifierHub>("/hubs/notifier");
app.MapHub<ClientPortalHub>("/hubs/client-portal");
app.MapHub<SupportChatHub>("/hubs/support-chat");

app.Run();