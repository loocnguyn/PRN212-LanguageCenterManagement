using BusinessObjects;
using Microsoft.EntityFrameworkCore;

using System.Data;

namespace DataAccessObjects;

// ============================================================
//  EnrollmentDAO — links students to classes, plus the money side.
//  CONTENTS:
//    1. EnrollWithInvoice  — transactional: create enrollment + its invoice
//    2. CRUD               — GetAll/GetById/Save/Update/Delete
//    3. TransferClass      — move a student to another class in one tx
//    4. Queries            — by class, by student, by student+class
//    5. Lifecycle helpers  — lock enrollments, count by class
// ============================================================
public class EnrollmentDAO
{
    public static void EnrollWithInvoice(
        Enrollment enrollment,
        decimal tuitionFee,
        DateOnly dueDate,
        string note)
        => EnrollWithInvoice(
            enrollment,
            new InvoicePricingInfo
            {
                OriginalAmount = tuitionFee,
                FinalAmount = tuitionFee,
                DiscountStatus = "NONE"
            },
            dueDate,
            note);

    public static void EnrollWithInvoice(
        Enrollment enrollment,
        InvoicePricingInfo pricing,
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
                    OriginalAmount = pricing.OriginalAmount,
                    DiscountId = pricing.DiscountId,
                    DiscountAmount = pricing.DiscountAmount,
                    Amount = pricing.FinalAmount,
                    DiscountDeadline = pricing.DiscountDeadline,
                    DiscountStatus = pricing.DiscountStatus,
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

    public static void TransferClass(
        int oldEnrollmentId,
        int newClassId,
        decimal newTuitionFee,
        DateOnly dueDate,
        string note)
    {
        using var context = new LanguageCenterContext();
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var oldEnrollment = context.Enrollments
                .Include(e => e.Class)
                .ThenInclude(c => c.Course)
                .FirstOrDefault(e => e.EnrollmentId == oldEnrollmentId)
                ?? throw new InvalidOperationException($"Enrollment {oldEnrollmentId} not found.");

            if (oldEnrollment.Status != "ACTIVE")
                throw new InvalidOperationException("Only active enrollments can be transferred.");
            if (oldEnrollment.ClassId == newClassId)
                throw new InvalidOperationException("Please choose a different class to transfer.");

            var newClass = context.Classes
                .Include(c => c.Course)
                .FirstOrDefault(c => c.ClassId == newClassId)
                ?? throw new InvalidOperationException($"Class {newClassId} not found.");

            var activeCount = context.Enrollments
                .Count(e => e.ClassId == newClassId && e.Status == "ACTIVE");
            if (activeCount >= newClass.MaxStudents)
                throw new InvalidOperationException($"Class '{newClass.Name}' is full ({activeCount}/{newClass.MaxStudents}).");

            var targetEnrollment = context.Enrollments
                .FirstOrDefault(e => e.StudentId == oldEnrollment.StudentId && e.ClassId == newClassId);
            if (targetEnrollment != null && targetEnrollment.Status != "DROPPED")
                throw new InvalidOperationException(
                    $"Student {oldEnrollment.StudentId} is already enrolled in class '{newClass.Name}' (status: {targetEnrollment.Status}).");

            if (targetEnrollment == null)
            {
                targetEnrollment = new Enrollment
                {
                    StudentId = oldEnrollment.StudentId,
                    ClassId = newClassId,
                    EnrolledDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = "ACTIVE",
                    Note = note
                };
                context.Enrollments.Add(targetEnrollment);
                context.SaveChanges();
            }
            else
            {
                targetEnrollment.Status = "ACTIVE";
                targetEnrollment.EnrolledDate = DateOnly.FromDateTime(DateTime.Today);
                targetEnrollment.Note = note;
                context.SaveChanges();
            }

            var targetInvoices = context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.EnrollmentId == targetEnrollment.EnrollmentId && i.Status != "CANCELLED")
                .ToList();
            if (targetInvoices.Any(i => i.Payments.Any()))
                throw new InvalidOperationException("The target class already has invoice payments and cannot be overwritten.");
            foreach (var invoice in targetInvoices)
            {
                invoice.Status = "CANCELLED";
                invoice.Note = AppendNote(invoice.Note, "Cancelled because another enrollment invoice was transferred to this class.");
            }

            var sourceInvoice = context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.EnrollmentId == oldEnrollment.EnrollmentId && i.Status != "CANCELLED")
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefault();

            if (sourceInvoice == null)
            {
                context.Invoices.Add(new Invoice
                {
                    StudentId = oldEnrollment.StudentId,
                    EnrollmentId = targetEnrollment.EnrollmentId,
                    OriginalAmount = newTuitionFee,
                    DiscountAmount = 0,
                    Amount = newTuitionFee,
                    DiscountStatus = "NONE",
                    Status = "UNPAID",
                    DueDate = dueDate,
                    CreatedAt = DateTime.Now,
                    Note = $"Tuition fee for transferred class {newClass.Name}"
                });
            }
            else
            {
                var paidAmount = sourceInvoice.Payments.Sum(p => p.AmountPaid);
                var refundAmount = Math.Max(0, paidAmount - newTuitionFee);

                sourceInvoice.EnrollmentId = targetEnrollment.EnrollmentId;
                sourceInvoice.OriginalAmount = newTuitionFee;
                sourceInvoice.DiscountId = null;
                sourceInvoice.DiscountAmount = 0;
                sourceInvoice.DiscountDeadline = null;
                sourceInvoice.DiscountStatus = "NONE";
                sourceInvoice.Amount = newTuitionFee;
                sourceInvoice.DueDate ??= dueDate;
                sourceInvoice.Note = AppendNote(
                    sourceInvoice.Note,
                    $"Transferred from class {oldEnrollment.Class?.Name ?? oldEnrollment.ClassId.ToString()} to {newClass.Name}.");
                sourceInvoice.Status = CalculateInvoiceStatus(paidAmount, newTuitionFee);

                if (refundAmount > 0)
                {
                    var student = context.Students.Find(oldEnrollment.StudentId)
                        ?? throw new InvalidOperationException("Student not found.");
                    student.Balance += refundAmount;

                    context.WalletTransactions.Add(new WalletTransaction
                    {
                        StudentId = oldEnrollment.StudentId,
                        Amount = refundAmount,
                        TransactionType = "REFUND",
                        Description = $"Refund from class transfer, invoice #{sourceInvoice.InvoiceId}",
                        Status = "COMPLETED",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            oldEnrollment.Status = "TRANSFERRED";
            oldEnrollment.Note = AppendNote(oldEnrollment.Note, $"Transferred to class {newClass.Name}.");

            context.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static string AppendNote(string? original, string note)
    {
        if (string.IsNullOrWhiteSpace(original)) return note;
        var value = $"{original} | {note}";
        return value.Length <= 255 ? value : value[..255];
    }

    private static string CalculateInvoiceStatus(decimal paidAmount, decimal invoiceAmount)
    {
        if (paidAmount <= 0) return "UNPAID";
        return paidAmount < invoiceAmount ? "PARTIAL" : "PAID";
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
            .ThenInclude(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
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
