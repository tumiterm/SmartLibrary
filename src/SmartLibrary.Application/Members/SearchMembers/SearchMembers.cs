using MediatR;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Application.Members.SearchMembers;

public sealed record SearchMembersQuery(string? Search) : IRequest<IReadOnlyList<MemberDto>>;

public sealed class SearchMembersQueryHandler(IMemberRepository members)
    : IRequestHandler<SearchMembersQuery, IReadOnlyList<MemberDto>>
{
    public async Task<IReadOnlyList<MemberDto>> Handle(
        SearchMembersQuery request,
        CancellationToken cancellationToken)
    {
        var results = await members.SearchAsync(request.Search?.Trim(), limit: 50, cancellationToken);
        return [.. results.Select(MemberDto.FromEntity)];
    }
}
