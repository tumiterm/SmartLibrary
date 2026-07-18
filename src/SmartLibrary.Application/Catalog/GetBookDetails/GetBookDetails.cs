using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;

namespace SmartLibrary.Application.Catalog.GetBookDetails;

public sealed record GetBookDetailsQuery(Guid BookId) : IRequest<BookDetailsDto>;

public sealed class GetBookDetailsQueryHandler(IBookRepository books)
    : IRequestHandler<GetBookDetailsQuery, BookDetailsDto>
{
    public async Task<BookDetailsDto> Handle(GetBookDetailsQuery request, CancellationToken cancellationToken)
    {
        var book = await books.GetWithCopiesAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        return BookDetailsDto.FromEntity(book);
    }
}
