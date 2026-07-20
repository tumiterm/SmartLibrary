using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Opac;

public sealed record BranchAvailabilityDto(string BranchName, int Available, int Total);

/// <summary>
/// The patron-facing view of a title. Deliberately excludes everything private:
/// no borrower names, no loan history, no copy barcodes.
/// </summary>
public sealed record PublicBookDetailsDto(
    Guid Id,
    string? Isbn13,
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
    string Format,
    bool IsReferenceOnly,
    int CopiesTotal,
    int CopiesAvailable,
    int WaitlistCount,
    IReadOnlyList<BranchAvailabilityDto> Availability);

public sealed record GetPublicBookQuery(Guid BookId) : IRequest<PublicBookDetailsDto>;

public sealed class GetPublicBookQueryHandler(
    IBookRepository books,
    IHoldRepository holds)
    : IRequestHandler<GetPublicBookQuery, PublicBookDetailsDto>
{
    public async Task<PublicBookDetailsDto> Handle(GetPublicBookQuery request, CancellationToken cancellationToken)
    {
        var book = await books.GetWithCopiesAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        var waiting = await holds.CountPendingByBookAsync(book.Id, cancellationToken);

        var availability = book.Copies
            .GroupBy(c => c.Branch?.Name ?? "Digital / unassigned")
            .Select(g => new BranchAvailabilityDto(
                g.Key,
                g.Count(c => c.Status == CopyStatus.Available),
                g.Count()))
            .OrderByDescending(a => a.Available)
            .ToList();

        return new PublicBookDetailsDto(
            book.Id,
            book.Isbn13,
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
            book.Format.ToString(),
            book.IsReferenceOnly,
            book.Copies.Count,
            book.Copies.Count(c => c.Status == CopyStatus.Available),
            waiting,
            availability);
    }
}
