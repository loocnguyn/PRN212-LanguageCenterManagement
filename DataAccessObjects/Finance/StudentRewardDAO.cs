using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// StudentRewardDAO — EF data access for granted scholarships (records + duplicate guard).
public class StudentRewardDAO
{
    public static List<StudentReward> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.StudentRewards
            .AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.Discount)
            .OrderByDescending(r => r.AwardedAt)
            .ToList();
    }

    public static List<StudentReward> GetBySemesterAndCourse(int semesterId, int courseId)
    {
        using var context = new LanguageCenterContext();
        return context.StudentRewards
            .AsNoTracking()
            .Where(r => r.SemesterId == semesterId && r.CourseId == courseId)
            .ToList();
    }

    /// <summary>True if this student was already rewarded for this course in this semester.</summary>
    public static bool Exists(int studentId, int semesterId, int courseId)
    {
        using var context = new LanguageCenterContext();
        return context.StudentRewards
            .Any(r => r.StudentId == studentId && r.SemesterId == semesterId && r.CourseId == courseId);
    }

    public static void Save(StudentReward entity)
    {
        using var context = new LanguageCenterContext();
        context.StudentRewards.Add(entity);
        context.SaveChanges();
    }
}
