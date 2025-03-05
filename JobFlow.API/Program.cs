using FluentValidation;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Validators;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var deploymentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{deploymentEnvironment}.json", optional: true)
              .AddEnvironmentVariables()
              .Build();

var connectionStrings = builder.Configuration.GetSection("ConnectionString");
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

builder.Services.AddScoped<IUnitOfWork, JobFlowUnitOfWork>();
builder.Services.AddScoped<IOrganizationService, OrganizationalService>();

builder.Services.AddHttpContextAccessor();
var app = builder.Build();

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
