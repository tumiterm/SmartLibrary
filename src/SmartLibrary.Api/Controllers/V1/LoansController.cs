using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Circulation;
using SmartLibrary.Application.Circulation.CheckoutBook;
using SmartLibrary.Application.Circulation.GetActiveLoans;
using SmartLibrary.Application.Circulation.RenewLoan;
using SmartLibrary.Application.Circulation.ReturnBook;
using SmartLibrary.Application.Circulation.SettleFine;

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

    /// <summary>Checkout: scan the member's card, scan the copy's barcode.</summary>
    [HttpPost]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var loan = await sender.Send(
            new CheckoutBookCommand(request.MembershipNumber, request.Barcode),
            cancellationToken);

        return CreatedAtAction(nameof(GetActive), new { version = "1" }, loan);
    }

    /// <summary>Return: one barcode scan finds and closes the active loan; assesses an overdue fine when late.</summary>
    [HttpPost("return")]
    [ProducesResponseType<ReturnResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnResultDto>> Return(ReturnRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReturnBookCommand(request.Barcode), cancellationToken));

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
        Ok(await sender.Send(new SettleFineCommand(fineId, request.Waive), cancellationToken));
}

public sealed record CheckoutRequest(string MembershipNumber, string Barcode);

public sealed record ReturnRequest(string Barcode);

public sealed record SettleFineRequest(bool Waive = false);
