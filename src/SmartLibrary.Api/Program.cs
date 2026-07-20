using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using SmartLibrary.Api.Infrastructure;
using SmartLibrary.Application;
using SmartLibrary.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddOpenApi();

    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var signingKey = jwtSection["SigningKey"];
    if (string.IsNullOrWhiteSpace(signingKey))
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey is not configured. Set it via user secrets or environment variables.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddHealthChecks();

    // Tenant resolution: X-Tenant header for now; a JWT tenant-claim strategy is added
    // alongside token issuance. Tenants live in config until the EF-backed store lands.
    builder.Services.AddMultiTenant<TenantInfo>()
        .WithHeaderStrategy("X-Tenant")
        .WithConfigurationStore();

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseMultiTenant();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }
    else
    {
        // In dev the Vite proxy talks plain HTTP to :5205; redirecting those calls to
        // the https port turns them into cross-origin requests and breaks the SPA.
        app.UseHttpsRedirection();
    }
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "SmartLibrary API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposes the implicit Program class to WebApplicationFactory in integration tests.
public partial class Program;
