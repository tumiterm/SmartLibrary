using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog;

public sealed record BookCopyDto(
    Guid Id,
    string Barcode,
    string? ShelfNumber,
    string? CallNumber,
    Guid? BranchId,
    string? BranchName,
    string Status,
    string Condition,
    decimal? Price,
    DateTime AcquiredAtUtc,
    string? Notes);

public sealed record HoldQueueItemDto(
    Guid Id,
    string MemberName,
    string MembershipNumber,
    string Status,
    DateTime PlacedAtUtc,
    int Position);

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
    bool IsReferenceOnly,
    DateTime CreatedAtUtc,
    int CopiesTotal,
    int CopiesAvailable,
    bool IsLowStock,
    IReadOnlyList<BookCopyDto> Copies,
    IReadOnlyList<LoanSummaryDto> BorrowHistory,
    IReadOnlyList<HoldQueueItemDto> Holds)
{
    public static BookDetailsDto FromEntity(
        Book book,
        IReadOnlyList<LoanSummaryDto>? borrowHistory = null,
        IReadOnlyList<HoldQueueItemDto>? holds = null,
        int? lowStockThreshold = null)
    {
        var copies = book.Copies
            .OrderBy(c => c.AcquiredAtUtc)
            .Select(c => new BookCopyDto(
                c.Id,
                c.Barcode,
                c.ShelfNumber,
                c.CallNumber,
                c.BranchId,
                c.Branch?.Name,
                c.Status.ToString(),
                c.Condition.ToString(),
                c.Price,
                c.AcquiredAtUtc,
                c.Notes))
            .ToList();

        var available = book.Copies.Count(c => c.Status == CopyStatus.Available);

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
            book.IsReferenceOnly,
            book.CreatedAtUtc,
            CopiesTotal: copies.Count,
            CopiesAvailable: available,
            IsLowStock: lowStockThreshold is { } threshold && copies.Count > 0 && available <= threshold,
            Copies: copies,
            BorrowHistory: borrowHistory ?? [],
            Holds: holds ?? []);
    }
}
