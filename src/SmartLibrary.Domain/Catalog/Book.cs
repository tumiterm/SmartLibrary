using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Catalog;

/// <summary>
/// A bibliographic record — the tenant's local snapshot of book metadata.
/// External sources (Google Books) are only ever a data source; this row is the storage.
/// </summary>
public class Book : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized 13-digit ISBN, no hyphens. Null for manual entries without an ISBN.</summary>
    public string? Isbn13 { get; set; }

    /// <summary>Original 10-digit ISBN when known. Kept for reference/search only.</summary>
    public string? Isbn10 { get; set; }

    public required string Title { get; set; }

    public string? Subtitle { get; set; }

    public List<string> Authors { get; set; } = [];

    public string? Publisher { get; set; }

    /// <summary>Raw published date as reported by the source ("2015" or "2015-03-01").</summary>
    public string? PublishedDate { get; set; }

    public string? Description { get; set; }

    public int? PageCount { get; set; }

    /// <summary>ISO 639-1 language code, e.g. "en".</summary>
    public string? Language { get; set; }

    public List<string> Categories { get; set; } = [];

    public string? CoverImageUrl { get; set; }

    /// <summary>Library classification (Dewey, UDC, LCC...) applied by the cataloguer.</summary>
    public string? ClassificationNumber { get; set; }

    public BookFormat Format { get; set; } = BookFormat.Print;

    /// <summary>Reference-only material never leaves the library — checkout is refused.</summary>
    public bool IsReferenceOnly { get; set; }

    public MetadataSource MetadataSource { get; set; } = MetadataSource.Manual;

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public ICollection<BookCopy> Copies { get; set; } = [];
}
