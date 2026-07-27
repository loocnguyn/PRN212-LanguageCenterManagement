using BusinessObjects;

namespace Services;

// ============================================================
//  StudentRankingService — rank a course's students by their marks.
//
//  This is the surviving half of the old scholarship feature. The half that
//  granted tuition vouchers is gone on purpose: it wrote a TuitionDiscount and a
//  StudentReward row per student, which meant one click could mint dozens of
//  discounts that then had to be tracked, expired and reconciled against invoices.
//  Reading is safe and repeatable; awarding was not.
//
//  So this class only answers a question, and stores nothing.
//
//  CONTENTS:
//    1. GetRanking       — gather, average, sort, flag
//    2. WeightedAverage  — the shared grade formula
// ============================================================

public class StudentRankingService : IStudentRankingService
{
    private readonly IClassService _classService = new ClassService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeService _gradeService = new GradeService();

    // ---- 1. Ranking --------------------------------------------
    public List<StudentRanking> GetRanking(int semesterId, int courseId, decimal threshold)
    {
        // A course usually runs as one class per semester, but nothing stops it running
        // as two — so gather them all and rank across the lot.
        var classes = _classService.GetAll()
            .Where(c => c.SemesterId == semesterId && c.CourseId == courseId)
            .ToList();

        var rows = new List<StudentRanking>();

        foreach (var cls in classes)
        {
            var enrollments = _enrollmentService.GetByClassId(cls.ClassId)
                .Where(e => e.Status != "DROPPED")       // someone who left is not in the running
                .ToList();

            // One query for the whole class, then group in memory — not one per student.
            var gradesByEnrollment = _gradeService
                .GetByEnrollmentIds(enrollments.Select(e => e.EnrollmentId).ToList())
                .GroupBy(g => g.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var e in enrollments)
            {
                var grades = gradesByEnrollment.GetValueOrDefault(e.EnrollmentId) ?? new List<Grade>();
                var (average, weightCovered) = WeightedAverage(grades);

                rows.Add(new StudentRanking
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? $"Student #{e.StudentId}",
                    StudentEmail = e.Student?.Email ?? "",
                    ClassName = cls.Name,
                    AverageScore = average,
                    WeightCovered = weightCovered,
                    MeetsThreshold = average.HasValue && average.Value >= threshold
                });
            }
        }

        // Best first; the ungraded (null) sink to the bottom instead of ranking as zero.
        var ranked = rows.OrderByDescending(r => r.AverageScore ?? -1m)
                         .ThenBy(r => r.StudentName)
                         .ToList();

        for (int i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;
        return ranked;
    }

    // ---- 2. The formula ----------------------------------------
    /// <summary>
    /// The same rule the student and teacher grade screens use: normalise each component
    /// to /10, then weight it by the CLASS's frozen component weight. Returns the average
    /// and how much of the total weight was actually marked, so the caller can tell a
    /// final result from a partial one.
    /// </summary>
    private static (decimal? average, decimal weightCovered) WeightedAverage(List<Grade> grades)
    {
        decimal totalWeighted = 0, totalWeight = 0;

        foreach (var g in grades)
        {
            if (g.MaxScore <= 0 || g.Component == null) continue;

            var normalized = g.Score / g.MaxScore * 10m;
            totalWeighted += normalized * g.Component.WeightPercent;
            totalWeight += g.Component.WeightPercent;
        }

        return totalWeight > 0
            ? (Math.Round(totalWeighted / totalWeight, 2), totalWeight)
            : (null, 0);
    }
}
