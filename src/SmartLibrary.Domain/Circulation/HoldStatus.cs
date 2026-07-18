namespace SmartLibrary.Domain.Circulation;

public enum HoldStatus
{
    /// <summary>Waiting in the queue — no copy assigned yet.</summary>
    Pending = 0,

    /// <summary>A returned copy has been set aside for this member.</summary>
    Ready = 1,

    /// <summary>The member collected the book.</summary>
    Fulfilled = 2,

    Cancelled = 3,

    Expired = 4,
}
