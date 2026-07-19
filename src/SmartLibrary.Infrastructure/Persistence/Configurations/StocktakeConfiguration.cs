using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Inventory;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class StocktakeConfiguration : IEntityTypeConfiguration<Stocktake>
{
    public void Configure(EntityTypeBuilder<Stocktake> builder)
    {
        builder.IsMultiTenant();

        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.CreatedBy).HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).HasMaxLength(200);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Scans)
            .WithOne()
            .HasForeignKey(scan => scan.StocktakeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StocktakeScanConfiguration : IEntityTypeConfiguration<StocktakeScan>
{
    public void Configure(EntityTypeBuilder<StocktakeScan> builder)
    {
        builder.IsMultiTenant();

        builder.Property(s => s.CreatedBy).HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).HasMaxLength(200);

        builder.HasOne(s => s.BookCopy)
            .WithMany()
            .HasForeignKey(s => s.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.StocktakeId, s.BookCopyId }).IsUnique();
    }
}
