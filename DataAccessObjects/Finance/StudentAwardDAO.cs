using System.Data;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// ============================================================
//  StudentAwardDAO — paying a student for their results.
//  CONTENTS:
//    1. Award                  — credit the wallet and record the decision (tx)
//    2. GetRewardedStudentIds  — who already has one this semester
//    3. Lookups                — by semester, for a history screen
// ============================================================
public class StudentAwardDAO
{
    /// <summary>
    /// Credits <paramref name="amount"/> to the student's wallet and records why.
    ///
    /// Three writes, one transaction: the wallet row, the balance, and the award.
    /// If the award trips the UNIQUE constraint — somebody already pressed the
    /// button, or two machines pressed it at once — the money is rolled back with
    /// it. That ordering is the whole point: there is no window in which a student
    /// has been paid but no award says so.
    /// </summary>
    public static StudentAward Award(
        int studentId, int semesterId, decimal amount,
        decimal? averageScore, decimal? threshold, int? awardedBy, string? note)
    {
        if (amount <= 0)
            throw new InvalidOperationException("The award amount must be greater than 0.");

        using var context = new LanguageCenterContext();
        using var dbTransaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var student = context.Students.Find(studentId)
                ?? throw new InvalidOperationException($"Student {studentId} not found.");

            // Checked here as well as by the constraint, so the common case comes
            // back as a sentence rather than a SQL error.
            var already = context.StudentAwards
                .Any(a => a.StudentId == studentId && a.SemesterId == semesterId);
            if (already)
                throw new InvalidOperationException(
                    $"{student.FullName} has already been awarded for this semester.");

            var walletTransaction = new WalletTransaction
            {
                StudentId = studentId,
                Amount = amount,
                TransactionType = "REWARD",
                Description = note ?? "Academic award",
                Status = "COMPLETED",   // no payment gateway to wait for
                CreatedAt = DateTime.Now
            };
            context.WalletTransactions.Add(walletTransaction);
            student.Balance += amount;

            // SaveChanges first so the transaction gets its identity — the award
            // row cannot be built without it.
            context.SaveChanges();

            var award = new StudentAward
            {
                StudentId = studentId,
                SemesterId = semesterId,
                Amount = amount,
                AverageScore = averageScore,
                Threshold = threshold,
                TransactionId = walletTransaction.TransactionId,
                AwardedBy = awardedBy,
                AwardedAt = DateTime.Now,
                Note = note
            };
            context.StudentAwards.Add(award);

            context.SaveChanges();
            dbTransaction.Commit();
            return award;
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Everyone already awarded in this semester. A set rather than one lookup per
    /// student: the ranking screen asks about every row it displays at once.
    /// </summary>
    public static Dictionary<int, decimal> GetAwardedAmountsBySemester(int semesterId)
    {
        using var context = new LanguageCenterContext();
        return context.StudentAwards
            .Where(a => a.SemesterId == semesterId)
            .ToDictionary(a => a.StudentId, a => a.Amount);
    }

    public static List<StudentAward> GetBySemester(int semesterId)
    {
        using var context = new LanguageCenterContext();
        return context.StudentAwards
            .Include(a => a.Student)
            .Include(a => a.Semester)
            .Where(a => a.SemesterId == semesterId)
            .OrderByDescending(a => a.AwardedAt)
            .ToList();
    }
}
