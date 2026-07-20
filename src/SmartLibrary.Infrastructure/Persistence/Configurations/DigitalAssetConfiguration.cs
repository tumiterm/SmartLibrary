using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class DigitalAssetConfiguration : IEntityTypeConfiguration<DigitalAsset>
{
    public void Configure(EntityTypeBuilder<DigitalAsset> builder)
    {
        builder.IsMultiTenant();

        builder.Property(a => a.FileName).HasMaxLength(300).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(300).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(200);
        builder.Property(a => a.UpdatedBy).HasMaxLength(200);

        // One soft copy per title (per tenant — Finbuckle widens the index).
        builder.HasIndex(a => a.BookId).IsUnique();

        builder.HasOne(a => a.Book)
            .WithMany()
            .HasForeignKey(a => a.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
