using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Abstractions;

public interface ILoanRepository
{
    /// <summary>The single active (unreturned) loan for a copy, if any. Includes member + copy + book.</summary>
    Task<Loan?> GetActiveByCopyBarcodeAsync(string barcode, CancellationToken cancellationToken);

    Task<int> CountActiveByMemberAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>All loans for a member, newest first. Includes copy + book.</summary>
    Task<IReadOnlyList<Loan>> GetByMemberAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>Loans across all copies of a book, newest first. Includes member.</summary>
    Task<IReadOnlyList<Loan>> GetByBookAsync(Guid bookId, int limit, CancellationToken cancellationToken);

    /// <summary>Active loans, most recent first. Includes member + copy + book.</summary>
    Task<IReadOnlyList<Loan>> GetActiveAsync(int limit, CancellationToken cancellationToken);

    void Add(Loan loan);
}
