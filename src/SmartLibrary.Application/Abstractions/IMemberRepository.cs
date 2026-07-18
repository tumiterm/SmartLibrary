using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Abstractions;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Member?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<Member>> SearchAsync(string? search, int limit, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<bool> MembershipNumberExistsAsync(string membershipNumber, CancellationToken cancellationToken);

    void Add(Member member);
}
