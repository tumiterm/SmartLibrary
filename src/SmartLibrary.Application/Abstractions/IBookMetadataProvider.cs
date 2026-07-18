namespace SmartLibrary.Application.Abstractions;

/// <summary>
/// External bibliographic data source (Google Books). Used only to prefill metadata —
/// never as storage. Returns null when the source has no match for the ISBN.
/// </summary>
public interface IBookMetadataProvider
{
    Task<ExternalBookMetadata?> LookupByIsbn13Async(string isbn13, CancellationToken cancellationToken);
}

public sealed record ExternalBookMetadata(
    string? Isbn13,
    string? Isbn10,
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    string? Publisher,
    string? PublishedDate,
    string? Description,
    int? PageCount,
    string? Language,
    IReadOnlyList<string> Categories,
    string? CoverImageUrl);
