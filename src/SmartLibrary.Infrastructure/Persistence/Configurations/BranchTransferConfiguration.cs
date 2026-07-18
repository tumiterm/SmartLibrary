using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class BranchTransferConfiguration : IEntityTypeConfiguration<BranchTransfer>
{
    public void Configure(EntityTypeBuilder<BranchTransfer> builder)
    {
        builder.IsMultiTenant();

        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedBy).HasMaxLength(200);
        builder.Property(t => t.UpdatedBy).HasMaxLength(200);

        builder.HasOne(t => t.BookCopy)
            .WithMany()
            .HasForeignKey(t => t.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.FromBranch)
            .WithMany()
            .HasForeignKey(t => t.FromBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToBranch)
            .WithMany()
            .HasForeignKey(t => t.ToBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.BookCopyId, t.CompletedAtUtc });
    }
}
