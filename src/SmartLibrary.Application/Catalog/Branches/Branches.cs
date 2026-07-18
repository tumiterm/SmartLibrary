using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.Branches;

public sealed record BranchDto(Guid Id, string Name, string? Code, string? Address);

/* ── List ─────────────────────────────────────────────────────────────────── */

public sealed record GetBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;

public sealed class GetBranchesQueryHandler(IBranchRepository branches)
    : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchDto>>
{
    public async Task<IReadOnlyList<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var all = await branches.GetAllAsync(cancellationToken);
        return [.. all.Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Address))];
    }
}

/* ── Create ───────────────────────────────────────────────────────────────── */

public sealed record CreateBranchCommand(string Name, string? Code, string? Address) : IRequest<Guid>;

public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Code).MaximumLength(20);
        RuleFor(c => c.Address).MaximumLength(500);
    }
}

public sealed class CreateBranchCommandHandler(IBranchRepository branches, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBranchCommand, Guid>
{
    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await branches.NameExistsAsync(name, cancellationToken))
        {
            throw new ConflictException($"A branch named '{name}' already exists.");
        }

        var branch = new Branch
        {
            Name = name,
            Code = request.Code?.Trim(),
            Address = request.Address?.Trim(),
        };

        branches.Add(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }
}
