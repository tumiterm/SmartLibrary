using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Members;

namespace SmartLibrary.Application.Search;

public sealed record SearchBookHitDto(
    Guid Id,
    string Title,
    IReadOnlyList<string> Authors,
    string? Isbn13,
    string? CoverImageUrl);

public sealed record SearchCopyHitDto(
    string Barcode,
    Guid BookId,
    string BookTitle,
    string Status,
    string? BranchName);

public sealed record SearchMemberHitDto(
    Guid Id,
    string FullName,
    string MembershipNumber,
    string Email);

/// <summary>One query, three worlds: books (title/author/ISBN), copies (barcode), members (name/card/email).</summary>
public sealed record GlobalSearchResultDto(
    IReadOnlyList<SearchBookHitDto> Books,
    IReadOnlyList<SearchCopyHitDto> Copies,
    IReadOnlyList<SearchMemberHitDto> Members);

public sealed record GlobalSearchQuery(string Query) : IRequest<GlobalSearchResultDto>;

public sealed class GlobalSearchQueryHandler(
    IBookRepository books,
    IMemberRepository members)
    : IRequestHandler<GlobalSearchQuery, GlobalSearchResultDto>
{
    public async Task<GlobalSearchResultDto> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        if (query.Length < 2)
        {
            return new GlobalSearchResultDto([], [], []);
        }

        var (bookHits, _) = await books.SearchAsync(query, format: null, branchId: null, page: 1, pageSize: 5, cancellationToken);
        var copyHits = await books.SearchCopiesByBarcodeAsync(query, limit: 5, cancellationToken);
        var memberHits = await members.SearchAsync(query, limit: 5, cancellationToken);

        return new GlobalSearchResultDto(
            [.. bookHits.Select(b => new SearchBookHitDto(b.Id, b.Title, b.Authors, b.Isbn13, b.CoverImageUrl))],
            [.. copyHits.Select(c => new SearchCopyHitDto(
                c.Barcode, c.BookId, c.Book?.Title ?? "—", c.Status.ToString(), c.Branch?.Name))],
            [.. memberHits.Select(m => new SearchMemberHitDto(m.Id, m.FullName, m.MembershipNumber, m.Email))]);
    }
}
