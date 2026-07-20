using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Catalog;

/// <summary>
/// The uploaded soft copy of a title (PDF v1). Stored on the server per tenant and
/// only ever streamed into the in-app reader — never exposed as a download.
/// </summary>
public class DigitalAsset : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookId { get; set; }

    public Book? Book { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Relative path inside the tenant's digital storage root.</summary>
    public required string StoragePath { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
