using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Members.UpdateMember;

/* ── Edit details ─────────────────────────────────────────────────────────── */

public sealed record UpdateMemberCommand(
    Guid MemberId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    MemberType Type,
    Guid? HomeBranchId) : IRequest<MemberDto>;

public sealed class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.Phone).MaximumLength(30);
        RuleFor(c => c.Type).IsInEnum();
    }
}

public sealed class UpdateMemberCommandHandler(
    IMemberRepository members,
    IBranchRepository branches,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new NotFoundException($"Member {request.MemberId} was not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(member.Email, email, StringComparison.OrdinalIgnoreCase)
            && await members.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException($"A member with email {email} is already registered.");
        }

        if (request.HomeBranchId is { } branchId && !await branches.ExistsAsync(branchId, cancellationToken))
        {
            throw new NotFoundException($"Branch {branchId} was not found.");
        }

        member.FirstName = request.FirstName.Trim();
        member.LastName = request.LastName.Trim();
        member.Email = email;
        member.Phone = request.Phone?.Trim();
        member.Type = request.Type;
        member.HomeBranchId = request.HomeBranchId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MemberDto.FromEntity(member);
    }
}

/* ── Suspend / reactivate ─────────────────────────────────────────────────── */

public sealed record SetMemberStatusCommand(Guid MemberId, MemberStatus Status) : IRequest<MemberDto>;

public sealed class SetMemberStatusCommandHandler(IMemberRepository members, IUnitOfWork unitOfWork)
    : IRequestHandler<SetMemberStatusCommand, MemberDto>
{
    public async Task<MemberDto> Handle(SetMemberStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.Status is not (MemberStatus.Active or MemberStatus.Suspended))
        {
            throw new ConflictException($"{request.Status} is not a manual status.");
        }

        var member = await members.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new NotFoundException($"Member {request.MemberId} was not found.");

        member.Status = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MemberDto.FromEntity(member);
    }
}
