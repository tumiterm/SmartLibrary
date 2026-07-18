using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Members.RegisterMember;

public sealed record RegisterMemberCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    MemberType Type,
    Guid? HomeBranchId) : IRequest<MemberDto>;

public sealed class RegisterMemberCommandValidator : AbstractValidator<RegisterMemberCommand>
{
    public RegisterMemberCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.Phone).MaximumLength(30);
        RuleFor(c => c.Type).IsInEnum();
    }
}

public sealed class RegisterMemberCommandHandler(
    IMemberRepository members,
    IBranchRepository branches,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(RegisterMemberCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await members.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException($"A member with email {email} is already registered.");
        }

        if (request.HomeBranchId is { } branchId && !await branches.ExistsAsync(branchId, cancellationToken))
        {
            throw new NotFoundException($"Branch {branchId} was not found.");
        }

        var membershipNumber = await GenerateMembershipNumberAsync(cancellationToken);

        var member = new Member
        {
            MembershipNumber = membershipNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            Type = request.Type,
            HomeBranchId = request.HomeBranchId,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(1),
        };

        members.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MemberDto.FromEntity(member);
    }

    private async Task<string> GenerateMembershipNumberAsync(CancellationToken cancellationToken)
    {
        // M-<year>-<6 random digits>; unique per tenant, collision-checked.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"M-{DateTime.UtcNow.Year}-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
            if (!await members.MembershipNumberExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique membership number.");
    }
}
