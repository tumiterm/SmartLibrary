namespace SmartLibrary.Domain.Catalog;

public enum CopyStatus
{
    Available = 0,
    OnLoan = 1,
    OnHold = 2,
    InTransit = 3,
    Lost = 4,
    Damaged = 5,
    Withdrawn = 6,

    /// <summary>Expected on the shelf but not found at stocktake.</summary>
    Missing = 7,

    /// <summary>Permanently removed from the collection. History is never deleted.</summary>
    Disposed = 8,
}
