using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// StudentAwardRepository — thin pass-through from the service layer to StudentAwardDAO.

public class StudentAwardRepository : IStudentAwardRepository
{
    public StudentAward Award(int studentId, int semesterId, decimal amount,
        decimal? averageScore, decimal? threshold, int? awardedBy, string? note)
        => StudentAwardDAO.Award(studentId, semesterId, amount, averageScore, threshold, awardedBy, note);

    public Dictionary<int, decimal> GetAwardedAmountsBySemester(int semesterId)
        => StudentAwardDAO.GetAwardedAmountsBySemester(semesterId);

    public List<StudentAward> GetBySemester(int semesterId)
        => StudentAwardDAO.GetBySemester(semesterId);
}
