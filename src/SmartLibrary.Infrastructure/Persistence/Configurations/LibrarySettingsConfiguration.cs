using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLibrary.Domain.Settings;

namespace SmartLibrary.Infrastructure.Persistence.Configurations;

public sealed class LibrarySettingsConfiguration : IEntityTypeConfiguration<LibrarySettings>
{
    public void Configure(EntityTypeBuilder<LibrarySettings> builder)
    {
        builder.IsMultiTenant();

        builder.Property(s => s.DailyFineAmount).HasPrecision(10, 2);
        builder.Property(s => s.FineBlockThreshold).HasPrecision(10, 2);
        builder.Property(s => s.CreatedBy).HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).HasMaxLength(200);
    }
}
