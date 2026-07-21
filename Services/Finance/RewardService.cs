using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  RewardService — the scholarship engine.
//  Ranks a course's students by weighted average and turns the
//  eligible ones into per-student tuition-discount vouchers.
//  CONTENTS:
//    1. Dependencies         — the services/repo it composes
//    2. GetCandidates        — compute + rank the review list
//    3. GrantVouchers        — create a voucher per eligible student
//    4. GetHistory           — past awards
//    5. WeightedAverage      — shared grade-averaging formula
// ============================================================
public class RewardService : IRewardService
{
    // ---- 1. Dependencies ---------------------------------------
    private readonly IClassService _classService = new ClassService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeService _gradeService = new GradeService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ITuitionDiscountService _discountService = new TuitionDiscountService();
    private readonly IStudentRewardRepository _rewardRepo = new StudentRewardRepository();

    // ---- 2. Rank the candidates --------------------------------
    public List<RewardCandidate> GetCandidates(int semesterId, int courseId, decimal threshold)
    {
        // All classes that run this course in this semester (a student normally has one).
        var classes = _classService.GetAll()
            .Where(c => c.SemesterId == semesterId && c.CourseId == courseId)
            .ToList();

        var alreadyRewarded = _rewardRepo.GetBySemesterAndCourse(semesterId, courseId)
            .Select(r => r.StudentId)
            .ToHashSet();

        var list = new List<RewardCandidate>();
        foreach (var cls in classes)
        {
            var enrollments = _enrollmentService.GetByClassId(cls.ClassId);
            var enrollIds = enrollments.Select(e => e.EnrollmentId).ToList();

            // Batch-load all grades for the class at once (avoids N+1), grouped per enrollment.
            var gradesByEnroll = _gradeService.GetByEnrollmentIds(enrollIds)
                .GroupBy(g => g.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var e in enrollments)
            {
                var grades = gradesByEnroll.GetValueOrDefault(e.EnrollmentId) ?? new List<Grade>();
                var avg = WeightedAverage(grades);
                list.Add(new RewardCandidate
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? $"Student #{e.StudentId}",
                    ClassName = cls.Name,
                    AverageScore = avg,
                    IsEligible = avg.HasValue && avg.Value >= threshold,
                    AlreadyRewarded = alreadyRewarded.Contains(e.StudentId)
                });
            }
        }

        // Highest average first; students with no grades yet (null) sink to the bottom.
        var ranked = list.OrderByDescending(c => c.AverageScore ?? -1m).ToList();
        for (int i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;
        return ranked;
    }

    // ---- 3. Grant the vouchers ---------------------------------
    public int GrantVouchers(int semesterId, int courseId, decimal threshold, decimal discountPercent, int validDays)
    {
        var courseName = _courseService.GetById(courseId)?.Name ?? $"Course #{courseId}";
        var semesterName = _semesterService.GetById(semesterId)?.Name ?? $"Semester #{semesterId}";

        var eligible = GetCandidates(semesterId, courseId, threshold)
            .Where(c => c.IsEligible && !c.AlreadyRewarded)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        int granted = 0;

        foreach (var c in eligible)
        {
            // Re-check right before writing in case another pass already awarded this student.
            if (_rewardRepo.Exists(c.StudentId, semesterId, courseId)) continue;

            var discount = new TuitionDiscount
            {
                Code = $"RWD-{c.StudentId}-C{courseId}-S{semesterId}-{DateTime.Now:MMddHHmmss}",
                Name = $"Scholarship {discountPercent:0.##}% — {c.StudentName}",
                DiscountType = "PERCENT",
                DiscountValue = discountPercent,
                StartDate = today,
                EndDate = today.AddDays(validDays),
                IsActive = true,
                ConditionType = "NONE",
                Note = $"Reward for {courseName} in {semesterName} (avg {c.AverageDisplay}).",
                CreatedAt = DateTime.Now
            };
            _discountService.Save(discount); // EF fills discount.DiscountId after insert

            _rewardRepo.Save(new StudentReward
            {
                StudentId = c.StudentId,
                SemesterId = semesterId,
                CourseId = courseId,
                AverageScore = c.AverageScore ?? 0,
                DiscountId = discount.DiscountId,
                AwardedAt = DateTime.Now
            });
            granted++;
        }
        return granted;
    }

    // ---- 4. History --------------------------------------------
    public List<StudentReward> GetHistory() => _rewardRepo.GetAll();

    // ---- 5. Weighted-average formula ---------------------------
    // Same rule used on the student grade screens: normalize each component to /10, weight by
    // the CLASS's frozen component weight. Null when the student has no gradable component yet.
    private static decimal? WeightedAverage(List<Grade> grades)
    {
        decimal totalWeighted = 0, totalWeight = 0;
        foreach (var g in grades)
        {
            if (g.MaxScore > 0 && g.Component != null)
            {
                var normalized = g.Score / g.MaxScore * 10m;
                totalWeighted += normalized * g.Component.WeightPercent;
                totalWeight += g.Component.WeightPercent;
            }
        }
        return totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : (decimal?)null;
    }
}
