using SmartLibrary.Domain.Members;

namespace SmartLibrary.UnitTests.Members;

public class ReaderScoreTests
{
    [Fact]
    public void New_member_with_no_history_is_a_model_reader()
    {
        var result = ReaderScore.Calculate(0, 0, 0, 0m);

        Assert.Equal(100, result.Score);
        Assert.Equal("New Member", result.Tier);
        Assert.Single(result.Reasons);
    }

    [Fact]
    public void Perfect_record_scores_100_as_Laureate()
    {
        var result = ReaderScore.Calculate(returnedLoans: 12, lateReturns: 0, currentlyOverdue: 0, outstandingFines: 0m);

        Assert.Equal(100, result.Score);
        Assert.Equal("Laureate", result.Tier);
        Assert.Contains(result.Reasons, r => r.Contains("Perfect record", StringComparison.Ordinal));
    }

    [Fact]
    public void Occasional_lateness_on_a_long_record_costs_little()
    {
        // 1 late out of 20 → rate penalty ≈ 2 points.
        var result = ReaderScore.Calculate(20, 1, 0, 0m);

        Assert.InRange(result.Score, 95, 99);
        Assert.Equal("Laureate", result.Tier);
    }

    [Fact]
    public void Chronic_lateness_drops_the_tier()
    {
        // Half of 10 returns late → −23ish, plus one currently overdue −12.
        var result = ReaderScore.Calculate(10, 5, 1, 0m);

        Assert.InRange(result.Score, 55, 70);
        Assert.Equal("Reader", result.Tier);
        Assert.Equal(2, result.Reasons.Count);
    }

    [Fact]
    public void Outstanding_fines_and_overdues_can_sink_to_Truant()
    {
        var result = ReaderScore.Calculate(
            returnedLoans: 6,
            lateReturns: 6,
            currentlyOverdue: 2,
            outstandingFines: 150m);

        // 100 − 45 (all late) − 24 (2 overdue) − 30 (fines cap) = 1.
        Assert.InRange(result.Score, 0, 29);
        Assert.Equal("Truant", result.Tier);
        Assert.Equal(3, result.Reasons.Count);
    }

    [Fact]
    public void Every_deduction_is_explained()
    {
        var result = ReaderScore.Calculate(8, 2, 1, 25m);

        Assert.Contains(result.Reasons, r => r.Contains("2 of 8", StringComparison.Ordinal));
        Assert.Contains(result.Reasons, r => r.Contains("currently overdue", StringComparison.Ordinal));
        Assert.Contains(result.Reasons, r => r.Contains("unpaid fines", StringComparison.Ordinal));
    }
}
