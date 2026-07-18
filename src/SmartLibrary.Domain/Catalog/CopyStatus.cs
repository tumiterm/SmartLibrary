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
}
