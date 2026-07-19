namespace BusinessObjects;

// RewardCandidate — a computed, grid-facing row for the Scholarship Review screen.
// Not a database entity: RewardService builds these on the fly by ranking a course's
// students by their weighted average and flagging who qualifies / was already rewarded.
public class RewardCandidate
{
    public int Rank { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string ClassName { get; set; } = "";

    /// <summary>Weighted average (0–10), or null if the student has no gradable scores yet.</summary>
    public decimal? AverageScore { get; set; }

    public bool IsEligible { get; set; }
    public bool AlreadyRewarded { get; set; }

    public string AverageDisplay => AverageScore.HasValue ? AverageScore.Value.ToString("0.00") : "N/A";

    public string StatusDisplay =>
        AlreadyRewarded ? "Rewarded" : IsEligible ? "Eligible" : "—";
}
