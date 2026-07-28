using BusinessObjects;

namespace Repositories;

// IStudentAwardRepository — repository contract for StudentAward persistence.

public interface IStudentAwardRepository
{
    StudentAward Award(int studentId, int semesterId, decimal amount,
        decimal? averageScore, decimal? threshold, int? awardedBy, string? note);

    Dictionary<int, decimal> GetAwardedAmountsBySemester(int semesterId);

    List<StudentAward> GetBySemester(int semesterId);
}
