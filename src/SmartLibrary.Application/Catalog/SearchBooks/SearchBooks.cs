using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Models;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.SearchBooks;

public sealed record BookListItemDto(
    Guid Id,
    string? Isbn13,
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    string? CoverImageUrl,
    string Format,
    string? ClassificationNumber,
    int CopiesTotal,
    int CopiesAvailable);

public sealed record SearchBooksQuery(
    string? Search,
    BookFormat? Format,
    Guid? BranchId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<BookListItemDto>>;

public sealed class SearchBooksQueryValidator : AbstractValidator<SearchBooksQuery>
{
    public SearchBooksQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class SearchBooksQueryHandler(IBookRepository books)
    : IRequestHandler<SearchBooksQuery, PagedResult<BookListItemDto>>
{
    public async Task<PagedResult<BookListItemDto>> Handle(
        SearchBooksQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await books.SearchAsync(
            request.Search?.Trim(),
            request.Format,
            request.BranchId,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<BookListItemDto>(
            [.. items.Select(b => new BookListItemDto(
                b.Id,
                b.Isbn13,
                b.Title,
                b.Subtitle,
                b.Authors,
                b.CoverImageUrl,
                b.Format.ToString(),
                b.ClassificationNumber,
                b.Copies.Count,
                b.Copies.Count(c => c.Status == CopyStatus.Available)))],
            total,
            request.Page,
            request.PageSize);
    }
}
