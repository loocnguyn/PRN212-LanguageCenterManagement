using BusinessObjects;

namespace Services;

// IAwardService — service contract for paying students for their results.

public interface IAwardService
{
    /// <summary>Student id -> amount already awarded, for one semester.</summary>
    Dictionary<int, decimal> GetAwardedAmounts(int semesterId);

    List<StudentAward> GetBySemester(int semesterId);

    AwardBatchResult AwardMany(IList<int> studentIds, int semesterId, decimal amount,
        decimal threshold, int? awardedBy, string? note);
}
