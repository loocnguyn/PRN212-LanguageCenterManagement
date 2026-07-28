using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  AwardService — paying students for their results.
//
//  This is the half of the old scholarship feature that was removed, brought
//  back against the wallet instead of against tuition vouchers. A voucher had to
//  be tracked, expired and reconciled against invoices; money in a wallet is
//  already spendable, already visible to the student, and already has a ledger.
//
//  Awarding is the one irreversible thing this project does, so the rules are
//  all here rather than in the screen:
//    · only a student who actually qualifies may be paid;
//    · nobody is paid twice for the same semester;
//    · a failure part-way pays nobody it has not already reported.
//
//  CONTENTS:
//    1. GetAwardedAmounts — who already has one
//    2. AwardMany         — pay a batch, reporting per student
// ============================================================

public class AwardService : IAwardService
{
    private readonly IStudentAwardRepository _repo = new StudentAwardRepository();
    private readonly IStudentRankingService _rankingService = new StudentRankingService();

    /// <summary>Student id -> amount already awarded, for one semester.</summary>
    public Dictionary<int, decimal> GetAwardedAmounts(int semesterId)
        => _repo.GetAwardedAmountsBySemester(semesterId);

    public List<StudentAward> GetBySemester(int semesterId) => _repo.GetBySemester(semesterId);

    /// <summary>
    /// Awards the same amount to each student in <paramref name="studentIds"/>.
    ///
    /// Each student is a separate transaction, on purpose. One batch is not one
    /// decision — paying nineteen students should not be undone because the
    /// twentieth was already paid this morning by somebody else. The result says
    /// exactly who was paid and who was not, so the screen can report it rather
    /// than leaving the user to guess.
    /// </summary>
    public AwardBatchResult AwardMany(
        IList<int> studentIds, int semesterId, decimal amount, decimal threshold, int? awardedBy, string? note)
    {
        if (studentIds == null || studentIds.Count == 0)
            throw new InvalidOperationException("Pick at least one student to award.");

        if (amount <= 0)
            throw new InvalidOperationException("The award amount must be greater than 0.");

        // The ranking is re-read here rather than taken from the screen: the grid
        // may have been sitting open while a teacher entered the last mark, and an
        // award is not something to pay against a stale average.
        var ranking = _rankingService.GetRanking(semesterId, threshold)
            .Where(r => r.IsFullyMarked && r.MeetsThreshold)
            .ToDictionary(r => r.StudentId, r => r);

        var result = new AwardBatchResult();

        foreach (var studentId in studentIds.Distinct())
        {
            if (!ranking.TryGetValue(studentId, out var row))
            {
                result.Refused.Add("A selected student no longer clears the threshold — "
                                 + "their marks changed since the list was shown.");
                continue;
            }

            try
            {
                _repo.Award(studentId, semesterId, amount, row.AverageScore, threshold, awardedBy, note);
                result.Paid.Add(row.StudentName);
            }
            catch (InvalidOperationException ex)
            {
                // Already awarded, or the unique constraint caught a second press.
                result.Refused.Add(ex.Message);
            }
        }

        return result;
    }
}

/// <summary>What one batch actually did — named so the screen never has to guess.</summary>
public class AwardBatchResult
{
    public List<string> Paid { get; } = new();
    public List<string> Refused { get; } = new();
}
