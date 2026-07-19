using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Circulation;
using SmartLibrary.Application.Circulation.CheckoutBook;
using SmartLibrary.Application.Circulation.GetActiveLoans;
using SmartLibrary.Application.Circulation.RenewLoan;
using SmartLibrary.Application.Circulation.ReportLostBook;
using SmartLibrary.Application.Circulation.ReturnBook;
using SmartLibrary.Application.Circulation.SettleFine;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/loans")]
public sealed class LoansController(ISender sender) : ControllerBase
{
    /// <summary>Active loans, soonest due first.</summary>
    [HttpGet("active")]
    [ProducesResponseType<IReadOnlyList<LoanDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetActive(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetActiveLoansQuery(), cancellationToken));

    /// <summary>Checkout: scan the member's card, then one or many copy barcodes. Per-copy failures are reported alongside successes.</summary>
    [HttpPost]
    [ProducesResponseType<CheckoutResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutResultDto>> Checkout(
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CheckoutBooksCommand(request.MembershipNumber, request.Barcodes, request.BranchId),
            cancellationToken);

        return CreatedAtAction(nameof(GetActive), new { version = "1" }, result);
    }

    /// <summary>
    /// Return against the active loan: records when/where it came back, judges condition
    /// (normal or damaged with an optional charge), assesses overdue fines, closes the loan.
    /// The borrowing record is permanent.
    /// </summary>
    [HttpPost("return")]
    [ProducesResponseType<ReturnResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnResultDto>> Return(ReturnRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new ReturnBookCommand(request.Barcode, request.Outcome, request.Condition, request.DamageCharge, request.BranchId),
            cancellationToken));

    /// <summary>Writes a loaned copy off as lost: closes the loan, marks the copy Lost, raises a replacement charge.</summary>
    [HttpPost("lost")]
    [ProducesResponseType<LostBookResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LostBookResultDto>> ReportLost(
        ReportLostRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReportLostBookCommand(request.Barcode, request.ReplacementCharge), cancellationToken));

    /// <summary>Renew by barcode: refused when overdue, at the renewal limit, or when the waitlist has claims.</summary>
    [HttpPost("renew")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Renew(ReturnRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RenewLoanCommand(request.Barcode), cancellationToken));

    /// <summary>Settles a fine: pay it, or waive it at staff discretion.</summary>
    [HttpPost("fines/{fineId:guid}/settle")]
    [ProducesResponseType<FineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FineDto>> SettleFine(
        Guid fineId,
        SettleFineRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SettleFineCommand(fineId, request.Waive, request.Reason), cancellationToken));
}

public sealed record CheckoutRequest(
    string MembershipNumber,
    IReadOnlyList<string> Barcodes,
    Guid? BranchId = null);

public sealed record ReturnRequest(
    string Barcode,
    ReturnOutcome Outcome = ReturnOutcome.Normal,
    CopyCondition? Condition = null,
    decimal? DamageCharge = null,
    Guid? BranchId = null);

public sealed record ReportLostRequest(string Barcode, decimal? ReplacementCharge = null);

public sealed record SettleFineRequest(bool Waive = false, string? Reason = null);
