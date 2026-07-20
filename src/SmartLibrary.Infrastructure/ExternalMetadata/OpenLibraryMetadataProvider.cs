using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.ExternalMetadata;

/// <summary>
/// Open Library (openlibrary.org) — the second rung of the lookup chain, tried
/// when Google Books has no answer. Data source only; results are snapshotted.
/// </summary>
public sealed partial class OpenLibraryMetadataProvider(
    HttpClient httpClient,
    ILogger<OpenLibraryMetadataProvider> logger) : IBookMetadataProvider
{
    public async Task<ExternalBookMetadata?> LookupByIsbn13Async(string isbn13, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            new Uri($"api/books?bibkeys=ISBN:{Uri.EscapeDataString(isbn13)}&format=json&jscmd=data", UriKind.Relative),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogUnavailable(logger, (int)response.StatusCode, isbn13);
            return null;
        }

        var payload = await response.Content
            .ReadFromJsonAsync<Dictionary<string, OpenLibraryBook>>(cancellationToken);

        var book = payload?.GetValueOrDefault($"ISBN:{isbn13}");
        if (book is null || string.IsNullOrWhiteSpace(book.Title))
        {
            LogNoMatch(logger, isbn13);
            return null;
        }

        return new ExternalBookMetadata(
            Isbn13: isbn13,
            Isbn10: null,
            Title: book.Title!,
            Subtitle: book.Subtitle,
            Authors: [.. (book.Authors ?? []).Select(a => a.Name).OfType<string>()],
            Publisher: book.Publishers?.FirstOrDefault()?.Name,
            PublishedDate: book.PublishDate,
            Description: null,
            PageCount: book.NumberOfPages,
            Language: null,
            Categories: [.. (book.Subjects ?? []).Take(5).Select(s => s.Name).OfType<string>()],
            CoverImageUrl: book.Cover?.Medium ?? book.Cover?.Large ?? book.Cover?.Small,
            Source: MetadataSource.OpenLibrary);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Open Library unavailable (HTTP {StatusCode}) for ISBN {Isbn13}")]
    private static partial void LogUnavailable(ILogger logger, int statusCode, string isbn13);

    [LoggerMessage(Level = LogLevel.Information, Message = "Open Library has no match for ISBN {Isbn13}")]
    private static partial void LogNoMatch(ILogger logger, string isbn13);
}

internal sealed class OpenLibraryBook
{
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string? Title { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("authors")]
    public List<OpenLibraryNamed>? Authors { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("publishers")]
    public List<OpenLibraryNamed>? Publishers { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("publish_date")]
    public string? PublishDate { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("number_of_pages")]
    public int? NumberOfPages { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("subjects")]
    public List<OpenLibraryNamed>? Subjects { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cover")]
    public OpenLibraryCover? Cover { get; set; }
}

internal sealed class OpenLibraryNamed
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class OpenLibraryCover
{
    [System.Text.Json.Serialization.JsonPropertyName("small")]
    public string? Small { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("large")]
    public string? Large { get; set; }
}

/// <summary>Tries each source in order; the first hit wins.</summary>
public sealed class CompositeMetadataProvider(IReadOnlyList<IBookMetadataProvider> providers) : IBookMetadataProvider
{
    public async Task<ExternalBookMetadata?> LookupByIsbn13Async(string isbn13, CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            var result = await provider.LookupByIsbn13Async(isbn13, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
