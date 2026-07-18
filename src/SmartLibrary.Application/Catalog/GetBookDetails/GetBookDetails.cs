using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;

namespace SmartLibrary.Application.Catalog.GetBookDetails;

public sealed record GetBookDetailsQuery(Guid BookId) : IRequest<BookDetailsDto>;

public sealed class GetBookDetailsQueryHandler(
    IBookRepository books,
    ILoanRepository loans,
    IHoldRepository holds)
    : IRequestHandler<GetBookDetailsQuery, BookDetailsDto>
{
    public async Task<BookDetailsDto> Handle(GetBookDetailsQuery request, CancellationToken cancellationToken)
    {
        var book = await books.GetWithCopiesAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        var history = await loans.GetByBookAsync(book.Id, limit: 20, cancellationToken);
        var queue = await holds.GetQueueByBookAsync(book.Id, cancellationToken);

        return BookDetailsDto.FromEntity(
            book,
            [.. history.Select(l => new LoanSummaryDto(
                l.Id,
                l.Member?.FullName ?? "—",
                l.BorrowedAtUtc,
                l.DueAtUtc,
                l.ReturnedAtUtc))],
            [.. queue.Select((h, i) => new HoldQueueItemDto(
                h.Id,
                h.Member?.FullName ?? "—",
                h.Member?.MembershipNumber ?? "—",
                h.Status.ToString(),
                h.PlacedAtUtc,
                i + 1))]);
    }
}
