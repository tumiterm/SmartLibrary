namespace SmartLibrary.Domain.Common;

/// <summary>
/// Audit stamp applied automatically at SaveChanges time — handlers never set these.
/// CreatedBy/UpdatedBy hold the acting user's name (hardcoded until auth lands).
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }

    string? CreatedBy { get; set; }

    DateTime? UpdatedAtUtc { get; set; }

    string? UpdatedBy { get; set; }
}
