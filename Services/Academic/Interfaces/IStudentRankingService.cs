using BusinessObjects;

namespace Services;

// IStudentRankingService — who is doing well, and who clears a given mark.

public interface IStudentRankingService
{
    /// <summary>
    /// Every student taking <paramref name="courseId"/> in <paramref name="semesterId"/>,
    /// ranked by weighted average, best first. Students with no marks yet sink to the
    /// bottom rather than being dropped — an empty row is information too.
    ///
    /// <paramref name="threshold"/> only flags rows (MeetsThreshold); it never removes
    /// them. Filtering the list down is the caller's decision, so the same call serves
    /// both "show me everyone" and "show me the top ones".
    /// </summary>
    List<StudentRanking> GetRanking(int semesterId, int courseId, decimal threshold);
}
