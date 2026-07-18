namespace SmartLibrary.Domain.Catalog;

/// <summary>
/// A physical (or licensed digital) copy of a <see cref="Book"/> that can circulate.
/// </summary>
public class BookCopy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookId { get; set; }

    public Book? Book { get; set; }

    public required string Barcode { get; set; }

    public string? CallNumber { get; set; }

    /// <summary>Physical shelf identifier within the branch (physical copies only).</summary>
    public string? ShelfNumber { get; set; }

    /// <summary>Branch/shelf location. Becomes a proper Branch entity when locations are modeled.</summary>
    public string? Location { get; set; }

    /// <summary>Acquisition/replacement price, if tracked.</summary>
    public decimal? Price { get; set; }

    public CopyStatus Status { get; set; } = CopyStatus.Available;

    public CopyCondition Condition { get; set; } = CopyCondition.Good;

    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
