using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;
using SmartLibrary.Domain.Inventory;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Infrastructure.Persistence;

/// <summary>
/// Tenant-aware DbContext. Entities marked IsMultiTenant() get a TenantId column,
/// a global query filter on the current tenant, and tenant-adjusted unique indexes.
/// </summary>
public class AppDbContext : MultiTenantDbContext, IUnitOfWork
{
    public AppDbContext(IMultiTenantContextAccessor multiTenantContextAccessor, DbContextOptions<AppDbContext> options)
        : base(multiTenantContextAccessor, options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Fine> Fines => Set<Fine>();

    public DbSet<Hold> Holds => Set<Hold>();

    public DbSet<BranchTransfer> BranchTransfers => Set<BranchTransfer>();

    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
