using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.IsMultiTenant();

        builder.Property(m => m.MembershipNumber).HasMaxLength(20).IsRequired();
        builder.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(320).IsRequired();
        builder.Property(m => m.Phone).HasMaxLength(30);
        builder.Property(m => m.CreatedBy).HasMaxLength(200);
        builder.Property(m => m.UpdatedBy).HasMaxLength(200);

        builder.Ignore(m => m.FullName);

        // Unique per tenant — Finbuckle widens both with TenantId.
        builder.HasIndex(m => m.MembershipNumber).IsUnique();
        builder.HasIndex(m => m.Email).IsUnique();

        builder.HasOne(m => m.HomeBranch)
            .WithMany()
            .HasForeignKey(m => m.HomeBranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
