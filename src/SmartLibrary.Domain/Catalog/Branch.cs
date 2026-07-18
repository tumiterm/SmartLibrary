using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Catalog;

/// <summary>
/// A physical location of the tenant's library. Physical copies live at a branch;
/// digital items (PDF, e-magazine, e-newspaper) have no branch.
/// </summary>
public class Branch : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Short code used on labels and in reports, e.g. "MAIN", "WST".</summary>
    public string? Code { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
