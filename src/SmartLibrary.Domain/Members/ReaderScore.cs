using System.Globalization;

namespace SmartLibrary.Domain.Members;

public sealed record ReaderScoreResult(int Score, string Tier, IReadOnlyList<string> Reasons);

/// <summary>
/// The member's behavioural reputation, 0–100, computed from their circulation record.
/// Deliberately explainable: every deduction comes with a human-readable reason so
/// staff see *why* a patron scores what they score.
/// </summary>
public static class ReaderScore
{
    public static ReaderScoreResult Calculate(
        int returnedLoans,
        int lateReturns,
        int currentlyOverdue,
        decimal outstandingFines)
    {
        if (returnedLoans == 0 && currentlyOverdue == 0 && outstandingFines == 0)
        {
            return new ReaderScoreResult(
                100,
                "New Member",
                ["No borrowing history yet — everyone starts as a model reader."]);
        }

        var reasons = new List<string>();
        double score = 100;

        // Late-return habit: a rate, so occasional slips on a long record cost little.
        var onTimeRate = returnedLoans == 0 ? 1.0 : (returnedLoans - lateReturns) / (double)returnedLoans;
        var latePenalty = Math.Round((1 - onTimeRate) * 45);
        if (latePenalty > 0)
        {
            score -= latePenalty;
            reasons.Add($"{lateReturns} of {returnedLoans} returns were late (−{latePenalty})");
        }

        // Books overdue right now — the most actionable signal.
        var overduePenalty = Math.Min(currentlyOverdue * 12, 24);
        if (overduePenalty > 0)
        {
            score -= overduePenalty;
            reasons.Add(currentlyOverdue == 1
                ? $"1 book is currently overdue (−{overduePenalty})"
                : $"{currentlyOverdue} books are currently overdue (−{overduePenalty})");
        }

        // Unpaid fines: 1 point per 5.00 owed, capped.
        var finePenalty = Math.Min(Math.Round((double)outstandingFines / 5.0), 30);
        if (finePenalty > 0)
        {
            score -= finePenalty;
            reasons.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{outstandingFines:0.00} in unpaid fines (−{finePenalty})"));
        }

        if (reasons.Count == 0)
        {
            reasons.Add($"Perfect record — {returnedLoans} on-time returns and nothing outstanding.");
        }

        var final = (int)Math.Clamp(Math.Round(score), 0, 100);
        return new ReaderScoreResult(final, TierFor(final), reasons);
    }

    private static string TierFor(int score) => score switch
    {
        >= 90 => "Laureate",
        >= 75 => "Scholar",
        >= 55 => "Reader",
        >= 30 => "Drifter",
        _ => "Truant",
    };
}
