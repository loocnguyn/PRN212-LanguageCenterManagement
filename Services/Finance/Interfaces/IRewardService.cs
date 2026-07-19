using BusinessObjects;

namespace Services;

// IRewardService — scholarship review: rank a course's students and grant vouchers.
public interface IRewardService
{
    /// <summary>Ranks every active student of the given course (in the given semester) by their
    /// weighted average, high to low, flagging who clears the threshold and who was already rewarded.</summary>
    List<RewardCandidate> GetCandidates(int semesterId, int courseId, decimal threshold);

    /// <summary>Grants a percent-discount voucher to each eligible, not-yet-rewarded student.
    /// Returns the number of vouchers actually created.</summary>
    int GrantVouchers(int semesterId, int courseId, decimal threshold, decimal discountPercent, int validDays);

    List<StudentReward> GetHistory();
}
