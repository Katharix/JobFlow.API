using Azure.Identity;
using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using JobFlow.Business.ExternalServices.Twilio;
using JobFlow.Business.Models.ConfigurationInterfaces;
using JobFlow.Business.Models.ConfigurationModels;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Validators;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Middleware;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);
var deploymentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{deploymentEnvironment}.json", optional: true)
              .AddEnvironmentVariables()
              .Build();
var keyVaultValue = builder.Configuration.GetSection("KeyVaultUri").Value;
var keyVaultUri = new Uri(keyVaultValue);

builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());

var appConnectionString = builder.Configuration["SqlConnectionString"];

var jwtKey = builder.Configuration["JWTKey"];
var firebaseJson = builder.Configuration["Firebase-adminsdk"];
var firebaseCredentialPath = Path.Combine(builder.Environment.ContentRootPath, "job-flow-firebase-adminsdk.json");




FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromJson(firebaseJson)
});

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<OrganizationValidator> ();

// Add services to the container.
builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = false;
        });
builder.Services.AddProblemDetails();
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
    options.ApiKey = builder.Configuration[$"StripeSettings-ApiKey"];
    options.ReturnUrl = builder.Configuration[$"StripeSettings-ReturnUrl"];
    options.RefreshUrl = builder.Configuration[$"StripeSettings-RefreshUrl"];
});

builder.Services.Configure<TwilioSettings>(options =>
{
    options.SenderPhoneNumber = builder.Configuration[$"Twilio-SenderPhoneNumber"];
    options.AccountSId = builder.Configuration[$"Twilio-AccountSId"];
    options.AuthToken = builder.Configuration[$"Twilio-AuthToken"];
});

builder.Services.AddSingleton<ITwilioSettings>(sp =>
    sp.GetRequiredService<IOptions<TwilioSettings>>().Value
);
builder.Services.AddSingleton<IStripeSettings>(sp =>
    sp.GetRequiredService<IOptions<StripeSettings>>().Value
);


builder.Services.AddScoped<IUnitOfWork, JobFlowUnitOfWork>();
builder.Services.AddScoped<IOrganizationService, JobFlow.Business.Services.OrganizationService>();
builder.Services.AddScoped<IOrganizationTypeService, OrganizationTypeService>();
builder.Services.AddScoped<IOrganizationClientService, OrganizationClientService>();
builder.Services.AddScoped<IOrganizationServiceService, OrganizationServiceService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtKey))
    };
});

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
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseStatusCodePages();
app.UseAuthorization();
app.MapControllers();

app.Run();
