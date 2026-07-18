using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Members;

public sealed record MemberDto(
    Guid Id,
    string MembershipNumber,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    string Type,
    string Status,
    Guid? HomeBranchId,
    string? HomeBranchName,
    DateTime JoinedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc,
    string? CreatedBy)
{
    public static MemberDto FromEntity(Member member) => new(
        member.Id,
        member.MembershipNumber,
        member.FirstName,
        member.LastName,
        member.FullName,
        member.Email,
        member.Phone,
        member.Type.ToString(),
        member.Status.ToString(),
        member.HomeBranchId,
        member.HomeBranch?.Name,
        member.JoinedAtUtc,
        member.ExpiresAtUtc,
        member.CreatedAtUtc,
        member.CreatedBy);
}
