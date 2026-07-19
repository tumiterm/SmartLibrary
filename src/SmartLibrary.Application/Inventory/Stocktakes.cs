using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Inventory;

namespace SmartLibrary.Application.Inventory;

public sealed record StocktakeDto(
    Guid Id,
    Guid? BranchId,
    string? BranchName,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int ExpectedCount,
    int ScannedCount,
    int MissingCount,
    int FoundCount,
    string? StartedBy)
{
    public static StocktakeDto FromEntity(Stocktake stocktake) => new(
        stocktake.Id,
        stocktake.BranchId,
        stocktake.Branch?.Name,
        stocktake.Status.ToString(),
        stocktake.StartedAtUtc,
        stocktake.CompletedAtUtc,
        stocktake.ExpectedCount,
        stocktake.ScannedCount,
        stocktake.MissingCount,
        stocktake.FoundCount,
        stocktake.CreatedBy);
}

public sealed record StocktakeCopyDto(
    Guid CopyId,
    string Barcode,
    Guid BookId,
    string BookTitle,
    string Status,
    string? BranchName);

public sealed record ScanResultDto(
    StocktakeDto Stocktake,
    string Barcode,
    string BookTitle,
    /// <summary>Set when this scan recovered a copy previously Lost or Missing.</summary>
    bool WasFound,
    bool AlreadyScanned);

public sealed record StocktakeReportDto(
    StocktakeDto Stocktake,
    IReadOnlyList<StocktakeCopyDto> Missing,
    IReadOnlyList<StocktakeCopyDto> Found);

/* ── Start ────────────────────────────────────────────────────────────────── */

public sealed record StartStocktakeCommand(Guid? BranchId, string? Notes) : IRequest<StocktakeDto>;

public sealed class StartStocktakeCommandValidator : AbstractValidator<StartStocktakeCommand>
{
    public StartStocktakeCommandValidator()
    {
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}

public sealed class StartStocktakeCommandHandler(
    IStocktakeRepository stocktakes,
    IBranchRepository branches,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartStocktakeCommand, StocktakeDto>
{
    public async Task<StocktakeDto> Handle(StartStocktakeCommand request, CancellationToken cancellationToken)
    {
        if (await stocktakes.GetOpenAsync(cancellationToken) is not null)
        {
            throw new ConflictException("A stocktake is already open — complete or cancel it first.");
        }

        if (request.BranchId is { } branchId && !await branches.ExistsAsync(branchId, cancellationToken))
        {
            throw new NotFoundException($"Branch {branchId} was not found.");
        }

        var expected = await stocktakes.GetExpectedCopiesAsync(request.BranchId, cancellationToken);

        var stocktake = new Stocktake
        {
            BranchId = request.BranchId,
            Notes = request.Notes,
            ExpectedCount = expected.Count,
        };

        stocktakes.Add(stocktake);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return StocktakeDto.FromEntity(stocktake);
    }
}

/* ── Scan ─────────────────────────────────────────────────────────────────── */

public sealed record ScanStocktakeItemCommand(Guid StocktakeId, string Barcode) : IRequest<ScanResultDto>;

public sealed class ScanStocktakeItemCommandHandler(
    IStocktakeRepository stocktakes,
    IBookRepository books,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ScanStocktakeItemCommand, ScanResultDto>
{
    public async Task<ScanResultDto> Handle(ScanStocktakeItemCommand request, CancellationToken cancellationToken)
    {
        var stocktake = await stocktakes.GetByIdAsync(request.StocktakeId, cancellationToken)
            ?? throw new NotFoundException($"Stocktake {request.StocktakeId} was not found.");

        if (stocktake.Status != StocktakeStatus.Open)
        {
            throw new ConflictException($"This stocktake is {stocktake.Status}.");
        }

        var copy = await books.GetCopyByBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy has barcode {request.Barcode}.");

        if (await stocktakes.HasScanAsync(stocktake.Id, copy.Id, cancellationToken))
        {
            return new ScanResultDto(
                StocktakeDto.FromEntity(stocktake), copy.Barcode, copy.Book?.Title ?? "—",
                WasFound: false, AlreadyScanned: true);
        }

        // A scan of a written-off copy is a recovery — restore it on the spot.
        var wasFound = copy.Status is CopyStatus.Lost or CopyStatus.Missing;
        if (wasFound)
        {
            copy.Status = CopyStatus.Available;
            stocktake.FoundCount++;
        }

        stocktakes.AddScan(new StocktakeScan
        {
            StocktakeId = stocktake.Id,
            BookCopyId = copy.Id,
            WasFound = wasFound,
        });
        stocktake.ScannedCount++;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ScanResultDto(
            StocktakeDto.FromEntity(stocktake), copy.Barcode, copy.Book?.Title ?? "—",
            wasFound, AlreadyScanned: false);
    }
}

/* ── Complete ─────────────────────────────────────────────────────────────── */

public sealed record CompleteStocktakeCommand(Guid StocktakeId) : IRequest<StocktakeReportDto>;

public sealed class CompleteStocktakeCommandHandler(
    IStocktakeRepository stocktakes,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteStocktakeCommand, StocktakeReportDto>
{
    public async Task<StocktakeReportDto> Handle(CompleteStocktakeCommand request, CancellationToken cancellationToken)
    {
        var stocktake = await stocktakes.GetWithScansAsync(request.StocktakeId, cancellationToken)
            ?? throw new NotFoundException($"Stocktake {request.StocktakeId} was not found.");

        if (stocktake.Status != StocktakeStatus.Open)
        {
            throw new ConflictException($"This stocktake is {stocktake.Status}.");
        }

        var expected = await stocktakes.GetExpectedCopiesAsync(stocktake.BranchId, cancellationToken);
        var scannedIds = stocktake.Scans.Select(s => s.BookCopyId).ToHashSet();

        var missing = new List<StocktakeCopyDto>();
        foreach (var copy in expected.Where(c => !scannedIds.Contains(c.Id)))
        {
            // Not on the shelf when it should have been. Statuses are adjusted;
            // the copy row and its history remain forever.
            if (copy.Status is CopyStatus.Available or CopyStatus.OnHold or CopyStatus.Damaged)
            {
                copy.Status = CopyStatus.Missing;
            }

            missing.Add(ToCopyDto(copy));
        }

        var found = stocktake.Scans
            .Where(s => s.WasFound && s.BookCopy is not null)
            .Select(s => ToCopyDto(s.BookCopy!))
            .ToList();

        stocktake.Status = StocktakeStatus.Completed;
        stocktake.CompletedAtUtc = DateTime.UtcNow;
        stocktake.MissingCount = missing.Count;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StocktakeReportDto(StocktakeDto.FromEntity(stocktake), missing, found);
    }

    private static StocktakeCopyDto ToCopyDto(BookCopy copy) => new(
        copy.Id,
        copy.Barcode,
        copy.BookId,
        copy.Book?.Title ?? "—",
        copy.Status.ToString(),
        copy.Branch?.Name);
}

/* ── Queries ──────────────────────────────────────────────────────────────── */

public sealed record GetStocktakesQuery : IRequest<IReadOnlyList<StocktakeDto>>;

public sealed class GetStocktakesQueryHandler(IStocktakeRepository stocktakes)
    : IRequestHandler<GetStocktakesQuery, IReadOnlyList<StocktakeDto>>
{
    public async Task<IReadOnlyList<StocktakeDto>> Handle(
        GetStocktakesQuery request,
        CancellationToken cancellationToken)
    {
        var recent = await stocktakes.GetRecentAsync(limit: 20, cancellationToken);
        return [.. recent.Select(StocktakeDto.FromEntity)];
    }
}

public sealed record GetOpenStocktakeQuery : IRequest<StocktakeDto?>;

public sealed class GetOpenStocktakeQueryHandler(IStocktakeRepository stocktakes)
    : IRequestHandler<GetOpenStocktakeQuery, StocktakeDto?>
{
    public async Task<StocktakeDto?> Handle(GetOpenStocktakeQuery request, CancellationToken cancellationToken)
    {
        var open = await stocktakes.GetOpenAsync(cancellationToken);
        return open is null ? null : StocktakeDto.FromEntity(open);
    }
}
