using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.IsMultiTenant();

        builder.Property(b => b.Isbn13).HasMaxLength(13);
        builder.Property(b => b.Isbn10).HasMaxLength(10);
        builder.Property(b => b.Title).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Subtitle).HasMaxLength(500);
        builder.Property(b => b.Publisher).HasMaxLength(300);
        builder.Property(b => b.PublishedDate).HasMaxLength(20);
        builder.Property(b => b.Language).HasMaxLength(10);
        builder.Property(b => b.CoverImageUrl).HasMaxLength(2000);
        builder.Property(b => b.ClassificationNumber).HasMaxLength(50);

        // Primitive collections map to JSON columns; revisit when authority control lands.
        builder.PrimitiveCollection(b => b.Authors);
        builder.PrimitiveCollection(b => b.Categories);

        // Unique per tenant — Finbuckle adds TenantId to unique indexes automatically.
        builder.HasIndex(b => b.Isbn13)
            .IsUnique()
            .HasFilter("[Isbn13] IS NOT NULL");

        builder.HasMany(b => b.Copies)
            .WithOne(c => c.Book)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
