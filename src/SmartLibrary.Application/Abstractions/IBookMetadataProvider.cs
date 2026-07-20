using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Abstractions;

/// <summary>
/// External bibliographic data source (Google Books, OpenLibrary…). Used only to
/// prefill metadata — never as storage. Returns null when the source has no match.
/// Implementations are tried in order; the first hit wins and reports its Source.
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
    string? CoverImageUrl,
    MetadataSource Source);
