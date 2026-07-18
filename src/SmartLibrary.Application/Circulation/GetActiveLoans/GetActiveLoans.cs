using MediatR;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Application.Circulation.GetActiveLoans;

public sealed record GetActiveLoansQuery : IRequest<IReadOnlyList<LoanDto>>;

public sealed class GetActiveLoansQueryHandler(ILoanRepository loans)
    : IRequestHandler<GetActiveLoansQuery, IReadOnlyList<LoanDto>>
{
    public async Task<IReadOnlyList<LoanDto>> Handle(
        GetActiveLoansQuery request,
        CancellationToken cancellationToken)
    {
        var active = await loans.GetActiveAsync(limit: 100, cancellationToken);
        return [.. active.Select(LoanDto.FromEntity)];
    }
}
