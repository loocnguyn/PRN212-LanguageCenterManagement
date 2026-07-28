using System;

namespace BusinessObjects;

// ============================================================
//  StudentAward — a student was rewarded for their results in one semester.
//
//  Separate from WalletTransaction because the two record different facts: the
//  wallet says money MOVED, this says somebody DECIDED to move it, and why.
//
//  One row per student per semester, enforced by a UNIQUE constraint in the
//  database rather than by a screen remembering to check. Reading a ranking is
//  safe to repeat; paying against it is not.
// ============================================================
public partial class StudentAward
{
    public int AwardId { get; set; }

    public int StudentId { get; set; }

    public int SemesterId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// The weighted average that earned the award, frozen here. Marks keep moving
    /// afterwards — a teacher corrects one component and the average changes — but
    /// the reason money was paid must not move with them.
    /// </summary>
    public decimal? AverageScore { get; set; }

    /// <summary>The pass mark in force when the award was made.</summary>
    public decimal? Threshold { get; set; }

    /// <summary>The wallet row that actually credited the money. Written in the
    /// same transaction, so neither can exist without the other.</summary>
    public int TransactionId { get; set; }

    /// <summary>Which account pressed the button. Money needs a name against it.</summary>
    public int? AwardedBy { get; set; }

    public DateTime AwardedAt { get; set; }

    public string? Note { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;

    public virtual WalletTransaction Transaction { get; set; } = null!;

    // ---- Display helpers (null-safe for un-Included navigations) ----
    public string StudentName => Student?.FullName ?? "";
    public string SemesterName => Semester?.Name ?? "";
    public string AmountDisplay => $"{Amount:N0} đ";
}
