using System;

namespace BusinessObjects;

// StudentReward — a scholarship granted to a student for a course in a semester.
// One row per (student, semester, course); it links to the generated discount voucher
// so the same achievement is never rewarded twice.
public partial class StudentReward
{
    public int RewardId { get; set; }

    public int StudentId { get; set; }

    public int SemesterId { get; set; }

    public int CourseId { get; set; }

    /// <summary>The weighted average that qualified the student, snapshotted at award time.</summary>
    public decimal AverageScore { get; set; }

    /// <summary>The TuitionDiscount voucher generated for this reward.</summary>
    public int DiscountId { get; set; }

    public DateTime AwardedAt { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual TuitionDiscount Discount { get; set; } = null!;
}
