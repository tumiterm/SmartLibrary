using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartLibrary.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for `dotnet ef` commands, which run outside the request
/// pipeline and therefore have no resolved tenant.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=SmartLibrary;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            .Options;

        var tenantInfo = new TenantInfo { Id = "design-time", Identifier = "design-time", Name = "Design Time" };
        return MultiTenantDbContext.Create<AppDbContext, TenantInfo>(tenantInfo, options);
    }
}
