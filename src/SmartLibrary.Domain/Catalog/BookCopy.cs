using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Catalog;

/// <summary>
/// A physical (or licensed digital) copy of a <see cref="Book"/> that can circulate.
/// </summary>
public class BookCopy : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookId { get; set; }

    public Book? Book { get; set; }

    public required string Barcode { get; set; }

    public string? CallNumber { get; set; }

    /// <summary>Physical shelf identifier within the branch (physical copies only).</summary>
    public string? ShelfNumber { get; set; }

    /// <summary>The branch holding this copy. Null for digital items, which have no physical home.</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    /// <summary>Acquisition/replacement price, if tracked.</summary>
    public decimal? Price { get; set; }

    public CopyStatus Status { get; set; } = CopyStatus.Available;

    public CopyCondition Condition { get; set; } = CopyCondition.Good;

    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
