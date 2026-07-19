using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Inventory;

public enum StocktakeStatus
{
    Open = 0,
    Completed = 1,
    Cancelled = 2,
}

/// <summary>
/// A physical inventory count for one branch (or the whole library). Copies are
/// scanned in; completion reconciles the shelf against the catalog — unscanned
/// expected copies go Missing, scanned Lost/Missing copies are Found. History is
/// never deleted.
/// </summary>
public class Stocktake : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Null = whole library.</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public StocktakeStatus Status { get; set; } = StocktakeStatus.Open;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public string? Notes { get; set; }

    public int ExpectedCount { get; set; }

    public int ScannedCount { get; set; }

    public int MissingCount { get; set; }

    public int FoundCount { get; set; }

    public ICollection<StocktakeScan> Scans { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}

public class StocktakeScan : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StocktakeId { get; set; }

    public Guid BookCopyId { get; set; }

    public BookCopy? BookCopy { get; set; }

    public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>True when this scan recovered a copy previously written off as Lost/Missing.</summary>
    public bool WasFound { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
