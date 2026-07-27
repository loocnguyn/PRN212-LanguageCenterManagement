namespace BusinessObjects;

// StudentRanking — a computed, grid-facing row for the Top Students screen.
//
// NOT a database entity. StudentRankingService builds these on the fly by ranking a
// course's students on their weighted average. Nothing is stored: the ranking is a
// question you ask of the grades, and it changes the moment a teacher enters a mark.
// That is the whole point of the screen — it reports, it does not award anything.
public class StudentRanking
{
    public int Rank { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public string ClassName { get; set; } = "";

    /// <summary>Weighted average (0–10), or null when the student has no gradable score yet.</summary>
    public decimal? AverageScore { get; set; }

    /// <summary>
    /// How much of the class's grading structure has actually been marked. An average
    /// over 40% of the weights is not comparable with one over 100%, so the screen says so
    /// rather than quietly ranking them against each other.
    /// </summary>
    public decimal WeightCovered { get; set; }

    public bool MeetsThreshold { get; set; }

    public bool IsFullyMarked => WeightCovered >= 100;

    public string AverageDisplay => AverageScore.HasValue ? AverageScore.Value.ToString("0.00") : "—";

    public string ProgressDisplay => AverageScore.HasValue
        ? (IsFullyMarked ? "final" : $"{WeightCovered:0}% marked")
        : "no marks yet";
}
