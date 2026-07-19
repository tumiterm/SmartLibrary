using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.SetCopyStatus;

/// <summary>
/// Manual status changes: mark a copy Lost, Damaged or Withdrawn — or restore it to
/// Available. Circulation-owned states (OnLoan, OnHold, InTransit) must be resolved
/// through their own flows first.
/// </summary>
public sealed record SetCopyStatusCommand(Guid CopyId, CopyStatus Status) : IRequest;

public sealed class SetCopyStatusCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    : IRequestHandler<SetCopyStatusCommand>
{
    private static readonly CopyStatus[] ManualStatuses =
        [CopyStatus.Available, CopyStatus.Lost, CopyStatus.Damaged, CopyStatus.Withdrawn];

    public async Task Handle(SetCopyStatusCommand request, CancellationToken cancellationToken)
    {
        if (!ManualStatuses.Contains(request.Status))
        {
            throw new ConflictException($"{request.Status} is set by circulation, not manually.");
        }

        var copy = await books.GetCopyByIdAsync(request.CopyId, cancellationToken)
            ?? throw new NotFoundException($"Copy {request.CopyId} was not found.");

        if (!ManualStatuses.Contains(copy.Status))
        {
            throw new ConflictException(
                $"Copy {copy.Barcode} is {copy.Status} — resolve that through circulation first.");
        }

        copy.Status = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
