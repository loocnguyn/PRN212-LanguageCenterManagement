using BusinessObjects;

namespace Services;

// IStudentRankingService — who is doing well across a semester.

public interface IStudentRankingService
{
    /// <summary>
    /// Every student enrolled in <paramref name="semesterId"/>, in any class, ranked by
    /// weighted average, best first. One row per enrolment, so a student taking two
    /// classes appears twice — a result belongs to a class, not to a person.
    ///
    /// Nothing is filtered out here: students with no marks yet sink to the bottom and
    /// <paramref name="threshold"/> only sets MeetsThreshold. Deciding which rows to
    /// show is the caller's job, so one call serves every combination of the filters.
    /// </summary>
    List<StudentRanking> GetRanking(int semesterId, decimal threshold);
}
