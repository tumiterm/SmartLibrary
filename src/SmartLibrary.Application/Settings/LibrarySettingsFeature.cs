using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Settings;

namespace SmartLibrary.Application.Settings;

public sealed record LibrarySettingsDto(
    int LoanDays,
    decimal DailyFineAmount,
    int MaxActiveLoans,
    decimal FineBlockThreshold,
    int MaxRenewals,
    int HoldPickupDays,
    int LowStockThreshold,
    int MaxOverdueItems,
    bool IsCustomized);

/* ---- Read ---------------------------------------------------------------- */

public sealed record GetLibrarySettingsQuery : IRequest<LibrarySettingsDto>;

public sealed class GetLibrarySettingsQueryHandler(
    ILibrarySettingsRepository settings,
    ICirculationPolicyProvider policyProvider)
    : IRequestHandler<GetLibrarySettingsQuery, LibrarySettingsDto>
{
    public async Task<LibrarySettingsDto> Handle(
        GetLibrarySettingsQuery request,
        CancellationToken cancellationToken)
    {
        var custom = await settings.GetAsync(cancellationToken);
        var effective = await policyProvider.GetAsync(cancellationToken);

        return new LibrarySettingsDto(
            effective.LoanDays,
            effective.DailyFineAmount,
            effective.MaxActiveLoans,
            effective.FineBlockThreshold,
            effective.MaxRenewals,
            effective.HoldPickupDays,
            effective.LowStockThreshold,
            effective.MaxOverdueItems,
            IsCustomized: custom is not null);
    }
}

/* ---- Update -------------------------------------------------------------- */

public sealed record UpdateLibrarySettingsCommand(
    int LoanDays,
    decimal DailyFineAmount,
    int MaxActiveLoans,
    decimal FineBlockThreshold,
    int MaxRenewals,
    int HoldPickupDays,
    int LowStockThreshold,
    int MaxOverdueItems) : IRequest<LibrarySettingsDto>;

public sealed class UpdateLibrarySettingsCommandValidator : AbstractValidator<UpdateLibrarySettingsCommand>
{
    public UpdateLibrarySettingsCommandValidator()
    {
        RuleFor(c => c.LoanDays).InclusiveBetween(1, 365);
        RuleFor(c => c.DailyFineAmount).InclusiveBetween(0, 10_000);
        RuleFor(c => c.MaxActiveLoans).InclusiveBetween(1, 100);
        RuleFor(c => c.FineBlockThreshold).InclusiveBetween(0, 1_000_000);
        RuleFor(c => c.MaxRenewals).InclusiveBetween(0, 20);
        RuleFor(c => c.HoldPickupDays).InclusiveBetween(1, 60);
        RuleFor(c => c.LowStockThreshold).InclusiveBetween(0, 100);
        RuleFor(c => c.MaxOverdueItems).InclusiveBetween(1, 100);
    }
}

public sealed class UpdateLibrarySettingsCommandHandler(
    ILibrarySettingsRepository settings,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLibrarySettingsCommand, LibrarySettingsDto>
{
    public async Task<LibrarySettingsDto> Handle(
        UpdateLibrarySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var row = await settings.GetAsync(cancellationToken);
        if (row is null)
        {
            row = new LibrarySettings();
            settings.Add(row);
        }

        row.LoanDays = request.LoanDays;
        row.DailyFineAmount = request.DailyFineAmount;
        row.MaxActiveLoans = request.MaxActiveLoans;
        row.FineBlockThreshold = request.FineBlockThreshold;
        row.MaxRenewals = request.MaxRenewals;
        row.HoldPickupDays = request.HoldPickupDays;
        row.LowStockThreshold = request.LowStockThreshold;
        row.MaxOverdueItems = request.MaxOverdueItems;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LibrarySettingsDto(
            row.LoanDays,
            row.DailyFineAmount,
            row.MaxActiveLoans,
            row.FineBlockThreshold,
            row.MaxRenewals,
            row.HoldPickupDays,
            row.LowStockThreshold,
            row.MaxOverdueItems,
            IsCustomized: true);
    }
}
