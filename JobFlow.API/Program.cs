using Azure.Identity;
using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using JobFlow.Business.Models;
using JobFlow.Business.Models.ConfigurationInterfaces;
using JobFlow.Business.Models.ConfigurationModels;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.PaymentGateways.Stripe;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Validators;
using JobFlow.Domain.Enums;
using JobFlow.Infrastructure.DI;
using JobFlow.Infrastructure.Extensions;
using JobFlow.Infrastructure.HttpClients;
using JobFlow.Infrastructure.Middleware;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;

// Start building config from files and env vars
builder.Configuration
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

//if (env.IsDevelopment())
//{
//    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
//    var firebaseCredentialPath = Path.Combine(builder.Environment.ContentRootPath, "job-flow-firebase-adminsdk.json");
//    FirebaseApp.Create(new AppOptions
//    {
//        Credential = GoogleCredential.FromFile(firebaseCredentialPath)
//    });
//}

// Build a temporary config just to get KeyVaultUri
var tempConfig = new ConfigurationBuilder()
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();


    var keyVaultUri = tempConfig["KeyVaultUri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
    }

var firebaseJson = builder.Configuration["Firebase-adminsdk"];
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromJson(firebaseJson)
});
// Always add environment variables last
builder.Configuration.AddEnvironmentVariables();
var jwtKey = builder.Configuration["JWTKey"];

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<OrganizationValidator>();

// Add services to the container.
builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = false;
        });
builder.Services.AddProblemDetails();

var appConnectionString = builder.Configuration["SqlConnectionString"];

builder.Services.AddDbContextFactory<JobFlowDbContext>(options => options.UseSqlServer(appConnectionString,
             b =>
             {
                 b.MigrationsAssembly("JobFlow.Infrastructure.Persistence");
                 b.CommandTimeout(170);
                 b.EnableRetryOnFailure(8, TimeSpan.FromSeconds(10), null);
             })
);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobFlow API", Version = "v1" });

    // Enable JWT authentication in Swagger
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
builder.Services.AddCors(op =>
{
    op.AddPolicy(name: apiAllowOrigins,
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(origin => true);
            policy.WithOrigins
            (
             "https://localhost:4200/",
             "http://localhost:4200/"
             );
        });
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

builder.Services.AddSingleton<ITwilioSettings>(sp =>
    sp.GetRequiredService<IOptions<TwilioSettings>>().Value
);
builder.Services.AddSingleton<IStripeSettings>(sp =>
    sp.GetRequiredService<IOptions<StripeSettings>>().Value
);
builder.Services.AddSingleton<IBrevoSettings>(sp =>
    sp.GetRequiredService<IOptions<BrevoSettings>>().Value
);
builder.Services.AddSingleton<IReCAPTCHASettings>(sp =>
    sp.GetRequiredService<IOptions<ReCAPTCHASettings>>().Value
);
builder.Services.AddSingleton<ISquareSettings>(sp =>
    sp.GetRequiredService<IOptions<SquareSettings>>().Value
);

builder.Services.AddJobFlowHttpClients();
builder.Services.AddAttributedServices(
    typeof(IJobFlowHttpClientFactory).Assembly,
    typeof(IUserService).Assembly,
    typeof(IUnitOfWork).Assembly,
    typeof(IPaymentProcessorFactory).Assembly,
    typeof(StripePaymentProcessor).Assembly
);


// Configure JWT Authentication
builder.Services.AddAuthentication();

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

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var test = scope.ServiceProvider.GetRequiredService<StripePaymentProcessor>();
    Console.WriteLine($"Resolved StripePaymentProcessor: {test.GetType().Name}");
}
StripeConfiguration.ApiKey = builder.Configuration[$"StripeSettings-ApiKey"];
// Configure the HTTP request pipeline.
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
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseStatusCodePages();
app.UseAuthorization();
app.MapControllers();

app.Run();
