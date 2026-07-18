using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Infrastructure.ExternalMetadata;
using SmartLibrary.Infrastructure.Persistence;
using SmartLibrary.Infrastructure.Persistence.Repositories;
using SmartLibrary.Infrastructure.Services;

namespace SmartLibrary.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IFineRepository, FineRepository>();
        services.AddScoped<IHoldRepository, HoldRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();

        services.Configure<CirculationOptions>(configuration.GetSection(CirculationOptions.SectionName));

        services.Configure<GoogleBooksOptions>(configuration.GetSection(GoogleBooksOptions.SectionName));
        services.AddHttpClient<IBookMetadataProvider, GoogleBooksMetadataProvider>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration[$"{GoogleBooksOptions.SectionName}:BaseUrl"] ?? "https://www.googleapis.com/books/v1/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
