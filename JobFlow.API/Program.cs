using FluentValidation;
using JobFlow.Business.Models.ConfigurationInterfaces;
using JobFlow.Business.Models.ConfigurationModels;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Validators;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);
var deploymentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{deploymentEnvironment}.json", optional: true)
              .AddEnvironmentVariables()
              .Build();

var connectionStrings = builder.Configuration.GetSection("ConnectionString");
var stripeApiKey = builder.Configuration.GetSection("StripeSettings").Get<StripeSettings>();
var appConnectionString = connectionStrings["JobFlowDB"].ToString();

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
                 b.CommandTimeout(150);
                 b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
             })
);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddScoped<IStripeSettings, StripeSettings>();
builder.Services.AddScoped<IUnitOfWork, JobFlowUnitOfWork>();
builder.Services.AddScoped<IOrganizationService, JobFlow.Business.Services.OrganizationService>();
builder.Services.AddScoped<IOrganizationTypeService, OrganizationTypeService>();
builder.Services.AddScoped<IOrganizationClientService, OrganizationClientService>();
builder.Services.AddScoped<IOrganizationServiceService, OrganizationServiceService>();

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<JobFlowDbContext>()
    .AddDefaultTokenProviders();

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
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
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
var app = builder.Build();

StripeConfiguration.ApiKey = stripeApiKey.ApiKey;
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

app.UseStatusCodePages();
app.UseAuthorization();
app.UseCors(apiAllowOrigins);
app.MapControllers();

app.Run();
