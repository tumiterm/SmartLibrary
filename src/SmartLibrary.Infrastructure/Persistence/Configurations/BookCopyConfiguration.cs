using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.IsMultiTenant();

        builder.Property(c => c.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CallNumber).HasMaxLength(100);
        builder.Property(c => c.ShelfNumber).HasMaxLength(50);
        builder.Property(c => c.Location).HasMaxLength(200);
        builder.Property(c => c.Price).HasPrecision(10, 2);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => c.Barcode).IsUnique();
    }
}
