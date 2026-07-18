using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.IsMultiTenant();

        builder.Property(l => l.CreatedBy).HasMaxLength(200);
        builder.Property(l => l.UpdatedBy).HasMaxLength(200);

        builder.HasOne(l => l.Member)
            .WithMany()
            .HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.BookCopy)
            .WithMany()
            .HasForeignKey(l => l.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fast "is this copy out?" and "member's active loans" lookups.
        builder.HasIndex(l => new { l.BookCopyId, l.ReturnedAtUtc });
        builder.HasIndex(l => new { l.MemberId, l.ReturnedAtUtc });
    }
}
