using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation;

public sealed record LoanDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string MembershipNumber,
    Guid BookCopyId,
    string Barcode,
    Guid? BookId,
    string BookTitle,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    int? DaysLate,
    bool IsOverdue)
{
    public static LoanDto FromEntity(Loan loan) => new(
        loan.Id,
        loan.MemberId,
        loan.Member?.FullName ?? "—",
        loan.Member?.MembershipNumber ?? "—",
        loan.BookCopyId,
        loan.BookCopy?.Barcode ?? "—",
        loan.BookCopy?.BookId,
        loan.BookCopy?.Book?.Title ?? "—",
        loan.BorrowedAtUtc,
        loan.DueAtUtc,
        loan.ReturnedAtUtc,
        loan.DaysLate,
        loan.IsOverdue(DateTime.UtcNow));
}

public sealed record FineDto(
    Guid Id,
    Guid MemberId,
    Guid? LoanId,
    string? BookTitle,
    decimal Amount,
    string Reason,
    string Status,
    DateTime AssessedAtUtc,
    DateTime? SettledAtUtc,
    string? Notes)
{
    public static FineDto FromEntity(Fine fine) => new(
        fine.Id,
        fine.MemberId,
        fine.LoanId,
        fine.Loan?.BookCopy?.Book?.Title,
        fine.Amount,
        fine.Reason.ToString(),
        fine.Status.ToString(),
        fine.AssessedAtUtc,
        fine.SettledAtUtc,
        fine.Notes);
}

/// <summary>Outcome of a return scan — includes any fines assessed at the desk.</summary>
/// <param name="HoldReadyFor">Set when the copy was claimed by the waitlist — the desk sets it aside for this member.</param>
/// <param name="ReturnedAtDifferentBranch">True when the receiving desk isn't the copy's home branch — send it back via transfer.</param>
public sealed record ReturnResultDto(
    LoanDto Loan,
    bool WasLate,
    int DaysLate,
    FineDto? FineAssessed,
    string? HoldReadyFor,
    string Outcome,
    IReadOnlyList<FineDto> FinesAssessed,
    bool ReturnedAtDifferentBranch,
    string? HomeBranchName);

/// <summary>Outcome of writing a loaned copy off as lost.</summary>
public sealed record LostBookResultDto(LoanDto Loan, FineDto? ReplacementCharge);
