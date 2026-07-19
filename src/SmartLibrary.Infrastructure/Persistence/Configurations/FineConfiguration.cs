using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.IsMultiTenant();

        builder.Property(f => f.Amount).HasPrecision(10, 2);
        builder.Property(f => f.Notes).HasMaxLength(500);
        builder.Property(f => f.CreatedBy).HasMaxLength(200);
        builder.Property(f => f.UpdatedBy).HasMaxLength(200);

        builder.HasOne(f => f.Member)
            .WithMany()
            .HasForeignKey(f => f.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Loan)
            .WithMany()
            .HasForeignKey(f => f.LoanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => new { f.MemberId, f.Status });
    }
}
