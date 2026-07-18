using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation.SettleFine;

public sealed record SettleFineCommand(Guid FineId, bool Waive) : IRequest<FineDto>;

public sealed class SettleFineCommandHandler(IFineRepository fines, IUnitOfWork unitOfWork)
    : IRequestHandler<SettleFineCommand, FineDto>
{
    public async Task<FineDto> Handle(SettleFineCommand request, CancellationToken cancellationToken)
    {
        var fine = await fines.GetByIdAsync(request.FineId, cancellationToken)
            ?? throw new NotFoundException($"Fine {request.FineId} was not found.");

        if (fine.Status != FineStatus.Outstanding)
        {
            throw new ConflictException($"This fine was already {fine.Status}.");
        }

        fine.Status = request.Waive ? FineStatus.Waived : FineStatus.Paid;
        fine.SettledAtUtc = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return FineDto.FromEntity(fine);
    }
}
