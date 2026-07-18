using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.IsMultiTenant();

        builder.Property(h => h.CreatedBy).HasMaxLength(200);
        builder.Property(h => h.UpdatedBy).HasMaxLength(200);

        builder.HasOne(h => h.Member)
            .WithMany()
            .HasForeignKey(h => h.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Book)
            .WithMany()
            .HasForeignKey(h => h.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.BookCopy)
            .WithMany()
            .HasForeignKey(h => h.BookCopyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => new { h.BookId, h.Status });
        builder.HasIndex(h => new { h.MemberId, h.Status });
    }
}
