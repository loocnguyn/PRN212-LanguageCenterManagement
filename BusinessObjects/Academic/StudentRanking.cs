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

    /// <summary>Which class they sat in — the ranking spans a whole semester now, so
    /// every row has to say what it is a result for.</summary>
    public string ClassName { get; set; } = "";

    public string CourseName { get; set; } = "";

    /// <summary>Weighted average (0–10), or null when the student has no gradable score yet.</summary>
    public decimal? AverageScore { get; set; }

    /// <summary>
    /// How much of the class's grading structure has actually been marked. An average
    /// over 40% of the weights is not comparable with one over 100%, so the screen says so
    /// rather than quietly ranking them against each other.
    /// </summary>
    public decimal WeightCovered { get; set; }

    public bool MeetsThreshold { get; set; }

    /// <summary>
    /// True once this student has been paid for this semester. Answered from the
    /// StudentAwards table, not from the grades — so the screen shows it before
    /// the button is pressed instead of after the database refuses.
    /// </summary>
    public bool IsAwarded { get; set; }

    /// <summary>How much they were paid, when <see cref="IsAwarded"/>.</summary>
    public decimal? AwardedAmount { get; set; }

    public string AwardDisplay => IsAwarded ? $"{AwardedAmount:N0} đ" : "—";

    /// <summary>
    /// Ticked in the award column. Plain settable bool with no change notification
    /// on purpose: the checkbox writes to it, and the only thing that ever writes
    /// back is a rebind of the whole grid, which redraws anyway.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>A row already paid cannot be ticked — the tickbox binds to this.</summary>
    public bool CanBeAwarded => !IsAwarded;

    public bool IsFullyMarked => WeightCovered >= 100;

    public string AverageDisplay => AverageScore.HasValue ? AverageScore.Value.ToString("0.00") : "—";

    public string ProgressDisplay => AverageScore.HasValue
        ? (IsFullyMarked ? "final" : $"{WeightCovered:0}% marked")
        : "no marks yet";
}
