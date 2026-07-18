using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog;

public sealed record BookCopyDto(
    Guid Id,
    string Barcode,
    string? ShelfNumber,
    string? CallNumber,
    string? Location,
    string Status,
    string Condition,
    decimal? Price,
    DateTime AcquiredAtUtc,
    string? Notes);

/// <summary>Placeholder loan shape until the circulation module lands — always empty for now.</summary>
public sealed record LoanSummaryDto(
    Guid Id,
    string PatronName,
    DateTime BorrowedAtUtc,
    DateTime? DueAtUtc,
    DateTime? ReturnedAtUtc);

public sealed record BookDetailsDto(
    Guid Id,
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
    string? ClassificationNumber,
    string Format,
    string MetadataSource,
    DateTime CreatedAtUtc,
    int CopiesTotal,
    int CopiesAvailable,
    IReadOnlyList<BookCopyDto> Copies,
    IReadOnlyList<LoanSummaryDto> BorrowHistory)
{
    public static BookDetailsDto FromEntity(Book book)
    {
        var copies = book.Copies
            .OrderBy(c => c.AcquiredAtUtc)
            .Select(c => new BookCopyDto(
                c.Id,
                c.Barcode,
                c.ShelfNumber,
                c.CallNumber,
                c.Location,
                c.Status.ToString(),
                c.Condition.ToString(),
                c.Price,
                c.AcquiredAtUtc,
                c.Notes))
            .ToList();

        return new BookDetailsDto(
            book.Id,
            book.Isbn13,
            book.Isbn10,
            book.Title,
            book.Subtitle,
            book.Authors,
            book.Publisher,
            book.PublishedDate,
            book.Description,
            book.PageCount,
            book.Language,
            book.Categories,
            book.CoverImageUrl,
            book.ClassificationNumber,
            book.Format.ToString(),
            book.MetadataSource.ToString(),
            book.CreatedAtUtc,
            CopiesTotal: copies.Count,
            CopiesAvailable: book.Copies.Count(c => c.Status == CopyStatus.Available),
            Copies: copies,
            BorrowHistory: []);
    }
}
