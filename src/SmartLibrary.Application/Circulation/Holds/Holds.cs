using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Circulation.Holds;

public sealed record HoldDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string MembershipNumber,
    Guid BookId,
    string BookTitle,
    string Status,
    DateTime PlacedAtUtc,
    DateTime? ReadyAtUtc,
    int? QueuePosition)
{
    public static HoldDto FromEntity(Hold hold, int? queuePosition = null) => new(
        hold.Id,
        hold.MemberId,
        hold.Member?.FullName ?? "—",
        hold.Member?.MembershipNumber ?? "—",
        hold.BookId,
        hold.Book?.Title ?? "—",
        hold.Status.ToString(),
        hold.PlacedAtUtc,
        hold.ReadyAtUtc,
        queuePosition);
}

/* ── Place ────────────────────────────────────────────────────────────────── */

public sealed record PlaceHoldCommand(Guid BookId, string MembershipNumber) : IRequest<HoldDto>;

public sealed class PlaceHoldCommandValidator : AbstractValidator<PlaceHoldCommand>
{
    public PlaceHoldCommandValidator()
    {
        RuleFor(c => c.BookId).NotEmpty();
        RuleFor(c => c.MembershipNumber).NotEmpty().MaximumLength(20);
    }
}

public sealed class PlaceHoldCommandHandler(
    IBookRepository books,
    IMemberRepository members,
    IHoldRepository holds,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PlaceHoldCommand, HoldDto>
{
    public async Task<HoldDto> Handle(PlaceHoldCommand request, CancellationToken cancellationToken)
    {
        var book = await books.GetWithCopiesAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        var member = await members.GetByMembershipNumberAsync(request.MembershipNumber.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No member holds card {request.MembershipNumber}.");

        if (member.Status != MemberStatus.Active)
        {
            throw new ConflictException($"{member.FullName}'s membership is {member.Status}.");
        }

        if (book.Copies.Count == 0)
        {
            throw new ConflictException("This book has no copies to hold yet.");
        }

        if (await holds.GetActiveByMemberAndBookAsync(member.Id, book.Id, cancellationToken) is not null)
        {
            throw new ConflictException($"{member.FullName} is already in the queue for this book.");
        }

        var hold = new Hold
        {
            MemberId = member.Id,
            Member = member,
            BookId = book.Id,
            Book = book,
        };

        holds.Add(hold);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var queue = await holds.GetQueueByBookAsync(book.Id, cancellationToken);
        var position = queue.ToList().FindIndex(h => h.Id == hold.Id) + 1;

        return HoldDto.FromEntity(hold, position == 0 ? null : position);
    }
}

/* ── Cancel ───────────────────────────────────────────────────────────────── */

public sealed record CancelHoldCommand(Guid HoldId) : IRequest<HoldDto>;

public sealed class CancelHoldCommandHandler(
    IHoldRepository holds,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelHoldCommand, HoldDto>
{
    public async Task<HoldDto> Handle(CancelHoldCommand request, CancellationToken cancellationToken)
    {
        var hold = await holds.GetByIdAsync(request.HoldId, cancellationToken)
            ?? throw new NotFoundException($"Hold {request.HoldId} was not found.");

        if (!hold.IsActive)
        {
            throw new ConflictException($"This hold is already {hold.Status}.");
        }

        var wasReady = hold.Status == HoldStatus.Ready;
        var copy = hold.BookCopy;

        hold.Status = HoldStatus.Cancelled;
        hold.ResolvedAtUtc = DateTime.UtcNow;

        // A cancelled Ready hold frees its copy — or passes it straight to the next waiter.
        if (wasReady && copy is not null)
        {
            var next = await holds.GetOldestPendingByBookAsync(hold.BookId, cancellationToken);
            if (next is not null)
            {
                next.Status = HoldStatus.Ready;
                next.BookCopyId = copy.Id;
                next.ReadyAtUtc = DateTime.UtcNow;
            }
            else
            {
                copy.Status = CopyStatus.Available;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HoldDto.FromEntity(hold);
    }
}
