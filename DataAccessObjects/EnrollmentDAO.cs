using BusinessObjects;
using Microsoft.EntityFrameworkCore;

using System.Data;

namespace DataAccessObjects;

public class EnrollmentDAO
{
    public static void EnrollWithInvoice(
        Enrollment enrollment,
        decimal tuitionFee,
        DateOnly dueDate,
        string note)
    {
        using var context = new LanguageCenterContext();
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            if (enrollment.EnrollmentId == 0)
            {
                context.Enrollments.Add(enrollment);
                context.SaveChanges();
            }
            else
            {
                var existing = context.Enrollments.Find(enrollment.EnrollmentId)
                    ?? throw new InvalidOperationException(
                        $"Enrollment {enrollment.EnrollmentId} not found.");
                existing.Status = enrollment.Status;
                existing.EnrolledDate = enrollment.EnrolledDate;
                context.SaveChanges();
            }

            var hasOpenInvoice = context.Invoices.Any(x =>
                x.EnrollmentId == enrollment.EnrollmentId
                && (x.Status == "UNPAID" || x.Status == "PARTIAL"));

            if (!hasOpenInvoice)
            {
                context.Invoices.Add(new Invoice
                {
                    StudentId = enrollment.StudentId,
                    EnrollmentId = enrollment.EnrollmentId,
                    Amount = tuitionFee,
                    Status = "UNPAID",
                    DueDate = dueDate,
                    CreatedAt = DateTime.Now,
                    Note = note
                });
                context.SaveChanges();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static List<Enrollment> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
            .ToList();
    }

    public static Enrollment? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments.FirstOrDefault(x => x.EnrollmentId == id);
    }

    public static void Save(Enrollment entity)
    {
        using var context = new LanguageCenterContext();
        context.Enrollments.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Enrollment entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Enrollments.Find(entity.EnrollmentId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Enrollments.Find(id);
        if (existing == null) return;
        context.Enrollments.Remove(existing);
        context.SaveChanges();
    }

    public static List<Enrollment> GetByClassId(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments
            .Where(e => e.ClassId == classId && e.Status == "ACTIVE")
            .Include(e => e.Student)
            .Include(e => e.Class)
            .ToList();
    }

    public static List<Enrollment> GetByStudentId(int studentId)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == "ACTIVE")
            .Include(e => e.Student)
            .Include(e => e.Class)
            .ThenInclude(c => c.Course)
            .Include(e => e.Class)
            .ThenInclude(c => c.Teacher)
            .Include(e => e.Class)
            .ThenInclude(c => c.Classroom)
            .Include(e => e.Class)
            .ThenInclude(c => c.Semester)
            .ToList();
    }

    public static Enrollment? GetByStudentAndClass(int studentId, int classId)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
            .FirstOrDefault(e => e.StudentId == studentId && e.ClassId == classId);
    }

    public static void LockEnrollmentsByClass(int classId)
    {
        using var context = new LanguageCenterContext();
        context.Enrollments
            .Where(e => e.ClassId == classId && e.Status == "ACTIVE")
            .ExecuteUpdate(e => e.SetProperty(x => x.Status, "LOCKED"));
    }

    public static int CountByClassId(int classId)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments.Count(e => e.ClassId == classId && e.Status == "ACTIVE");
    }
}
