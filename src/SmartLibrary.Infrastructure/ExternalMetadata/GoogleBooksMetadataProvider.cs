using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Infrastructure.ExternalMetadata;

public sealed class GoogleBooksOptions
{
    public const string SectionName = "GoogleBooks";

    public string BaseUrl { get; set; } = "https://www.googleapis.com/books/v1/";

    /// <summary>Optional two-letter country code sent as the `country` parameter; helps avoid regional quota blocks.</summary>
    public string? Country { get; set; }
}

/// <summary>
/// Google Books volumes API client. Data source only — results are snapshotted
/// into the local catalog by the add-book flow, never referenced live afterwards.
/// </summary>
public sealed partial class GoogleBooksMetadataProvider(
    HttpClient httpClient,
    IOptions<GoogleBooksOptions> options,
    ILogger<GoogleBooksMetadataProvider> logger) : IBookMetadataProvider
{
    public async Task<ExternalBookMetadata?> LookupByIsbn13Async(string isbn13, CancellationToken cancellationToken)
    {
        var query = $"volumes?q=isbn:{Uri.EscapeDataString(isbn13)}";
        if (!string.IsNullOrWhiteSpace(options.Value.Country))
        {
            query += $"&country={Uri.EscapeDataString(options.Value.Country)}";
        }

        // An unavailable external source (rate limit, outage) must degrade to the
        // manual-entry fallback, never fail the lookup request.
        using var httpResponse = await httpClient.GetAsync(new Uri(query, UriKind.Relative), cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            LogSourceUnavailable(logger, (int)httpResponse.StatusCode, isbn13);
            return null;
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<GoogleBooksVolumesResponse>(cancellationToken);

        var volumeInfo = response?.Items?.FirstOrDefault()?.VolumeInfo;
        if (volumeInfo is null || string.IsNullOrWhiteSpace(volumeInfo.Title))
        {
            LogNoUsableVolume(logger, isbn13);
            return null;
        }

        var identifiers = volumeInfo.IndustryIdentifiers ?? [];
        var isbn13FromSource = identifiers.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier;
        var isbn10FromSource = identifiers.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier;

        return new ExternalBookMetadata(
            Isbn13: isbn13FromSource ?? isbn13,
            Isbn10: isbn10FromSource,
            Title: volumeInfo.Title,
            Subtitle: volumeInfo.Subtitle,
            Authors: volumeInfo.Authors ?? [],
            Publisher: volumeInfo.Publisher,
            PublishedDate: volumeInfo.PublishedDate,
            Description: volumeInfo.Description,
            PageCount: volumeInfo.PageCount,
            Language: volumeInfo.Language,
            Categories: volumeInfo.Categories ?? [],
            CoverImageUrl: volumeInfo.ImageLinks?.Thumbnail ?? volumeInfo.ImageLinks?.SmallThumbnail,
            Source: SmartLibrary.Domain.Catalog.MetadataSource.GoogleBooks);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Google Books returned no usable volume for ISBN {Isbn13}")]
    private static partial void LogNoUsableVolume(ILogger logger, string isbn13);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google Books unavailable (HTTP {StatusCode}) for ISBN {Isbn13}; degrading to manual entry")]
    private static partial void LogSourceUnavailable(ILogger logger, int statusCode, string isbn13);
}

internal sealed class GoogleBooksVolumesResponse
{
    public int TotalItems { get; set; }

    public List<GoogleBooksVolume>? Items { get; set; }
}

internal sealed class GoogleBooksVolume
{
    public GoogleBooksVolumeInfo? VolumeInfo { get; set; }
}

internal sealed class GoogleBooksVolumeInfo
{
    public string? Title { get; set; }

    public string? Subtitle { get; set; }

    public List<string>? Authors { get; set; }

    public string? Publisher { get; set; }

    public string? PublishedDate { get; set; }

    public string? Description { get; set; }

    public int? PageCount { get; set; }

    public string? Language { get; set; }

    public List<string>? Categories { get; set; }

    public List<GoogleBooksIndustryIdentifier>? IndustryIdentifiers { get; set; }

    public GoogleBooksImageLinks? ImageLinks { get; set; }
}

internal sealed class GoogleBooksIndustryIdentifier
{
    public string? Type { get; set; }

    public string? Identifier { get; set; }
}

internal sealed class GoogleBooksImageLinks
{
    public string? SmallThumbnail { get; set; }

    public string? Thumbnail { get; set; }
}
