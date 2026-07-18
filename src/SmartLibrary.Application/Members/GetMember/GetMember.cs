using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Circulation;
using SmartLibrary.Application.Circulation.Holds;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Members.GetMember;

public sealed record ReaderScoreDto(int Score, string Tier, IReadOnlyList<string> Reasons);

/// <summary>The full patron profile: identity, circulation record, money owed, reputation.</summary>
public sealed record MemberProfileDto(
    MemberDto Member,
    IReadOnlyList<LoanDto> Loans,
    IReadOnlyList<FineDto> Fines,
    IReadOnlyList<HoldDto> Holds,
    decimal OutstandingFines,
    ReaderScoreDto ReaderScore);

public sealed record GetMemberQuery(Guid MemberId) : IRequest<MemberProfileDto>;

public sealed class GetMemberQueryHandler(
    IMemberRepository members,
    ILoanRepository loans,
    IFineRepository fines,
    IHoldRepository holds)
    : IRequestHandler<GetMemberQuery, MemberProfileDto>
{
    public async Task<MemberProfileDto> Handle(GetMemberQuery request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new NotFoundException($"Member {request.MemberId} was not found.");

        var memberLoans = await loans.GetByMemberAsync(member.Id, cancellationToken);
        var memberFines = await fines.GetByMemberAsync(member.Id, cancellationToken);
        var memberHolds = await holds.GetByMemberAsync(member.Id, cancellationToken);
        var outstanding = await fines.OutstandingTotalByMemberAsync(member.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var returned = memberLoans.Where(l => l.ReturnedAtUtc is not null).ToList();
        var score = ReaderScore.Calculate(
            returnedLoans: returned.Count,
            lateReturns: returned.Count(l => l.DaysLate > 0),
            currentlyOverdue: memberLoans.Count(l => l.IsOverdue(now)),
            outstandingFines: outstanding);

        return new MemberProfileDto(
            MemberDto.FromEntity(member),
            [.. memberLoans.Select(LoanDto.FromEntity)],
            [.. memberFines.Select(FineDto.FromEntity)],
            [.. memberHolds.Select(h => HoldDto.FromEntity(h))],
            outstanding,
            new ReaderScoreDto(score.Score, score.Tier, score.Reasons));
    }
}
