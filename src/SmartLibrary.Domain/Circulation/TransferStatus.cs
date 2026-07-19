namespace SmartLibrary.Domain.Circulation;

public enum TransferStatus
{
    /// <summary>Requested; the copy is pulled from circulation, awaiting the source branch's release.</summary>
    Requested = 0,

    /// <summary>Dispatched by the source branch; physically travelling.</summary>
    InTransit = 1,

    /// <summary>Receipt acknowledged by the destination branch.</summary>
    Received = 2,

    Rejected = 3,

    Cancelled = 4,

    LostInTransit = 5,

    DamagedInTransit = 6,
}