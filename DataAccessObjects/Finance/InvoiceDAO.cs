using BusinessObjects;

using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// ============================================================
//  InvoiceDAO — tuition invoices + the early-payment discount rules.
//  CONTENTS:
//    1. CRUD                     — GetAll/GetById/Save/Update/Delete
//    2. Search                   — by keyword / status
//    3. Discount lifecycle       — ApplyExpiredEarlyDiscounts,
//                                  LockEarlyDiscountIfPaidOnTime
//    4. Ownership / open-invoice — guards for a student/enrollment
//    5. Payment helpers          — GetPaidAmount, HasPayments,
//                                  CancelOpenByEnrollmentId
// ============================================================
public class InvoiceDAO
{
    public static List<Invoice> GetAll()
    {
        using var context = new LanguageCenterContext();
        ApplyExpiredEarlyDiscounts(context);
        return context.Invoices.Include(x => x.Student).Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.Course)
            .Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.ClassTeachers).ThenInclude(ct => ct.Teacher)
            .Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.Semester)
            .Include(x => x.Discount).Include(x => x.Payments).AsNoTracking().ToList();
    }

    public static Invoice? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        ApplyExpiredEarlyDiscounts(context);
        return context.Invoices.Include(x => x.Student).Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.Course)
            .Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.ClassTeachers).ThenInclude(ct => ct.Teacher)
            .Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.Semester)
            .Include(x => x.Discount).Include(x => x.Payments).AsNoTracking()
            .FirstOrDefault(x => x.InvoiceId == id);
    }

    public static void Save(Invoice entity)
    {
        using var context = new LanguageCenterContext();
        context.Invoices.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Invoice entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Invoices.Find(entity.InvoiceId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Invoices.Find(id);
        if (existing == null) return;
        context.Invoices.Remove(existing);
        context.SaveChanges();
    }
    public static List<Invoice> Search(string? keyword, string? status)
    {
        using var context = new LanguageCenterContext();
        ApplyExpiredEarlyDiscounts(context);
        var query = context.Invoices.Include(x => x.Student).Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.Course)
            .Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.ClassTeachers).ThenInclude(ct => ct.Teacher)
            .Include(x => x.Enrollment)
            .ThenInclude(x => x!.Class)
            .ThenInclude(x => x.Semester)
            .Include(x => x.Discount).Include(x => x.Payments).AsNoTracking().AsQueryable();
        keyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var isNumber = int.TryParse(keyword, out var number);
            query = query.Where(x => (isNumber && (x.InvoiceId == number || x.StudentId == number))
                || x.Student.FullName.Contains(keyword)
                || (x.Note != null && x.Note.Contains(keyword)));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(x => x.Status == status);
        return query.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public static void ApplyExpiredEarlyDiscounts(LanguageCenterContext context)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var invoices = context.Invoices
            .Include(x => x.Discount)
            .Include(x => x.Payments)
            .Where(x => x.DiscountStatus == "ACTIVE"
                && x.DiscountDeadline != null
                && x.DiscountDeadline < today
                && x.Discount != null
                && x.Discount.ConditionType == "EARLY_PAYMENT")
            .ToList();

        var changed = false;
        foreach (var invoice in invoices)
        {
            var paidByDeadline = invoice.Payments
                .Where(x => DateOnly.FromDateTime(x.PaidAt.Date) <= invoice.DiscountDeadline)
                .Sum(x => x.AmountPaid);

            if (paidByDeadline >= invoice.Amount)
            {
                invoice.DiscountStatus = "LOCKED";
            }
            else
            {
                invoice.Amount = invoice.OriginalAmount > 0 ? invoice.OriginalAmount : invoice.Amount + invoice.DiscountAmount;
                invoice.DiscountAmount = 0;
                invoice.DiscountStatus = "EXPIRED";
                invoice.Status = CalculateInvoiceStatus(invoice.Payments.Sum(x => x.AmountPaid), invoice.Amount);
                invoice.Note = AppendNote(invoice.Note, "Early payment discount expired.");
            }
            changed = true;
        }

        if (changed)
            context.SaveChanges();
    }

    public static void LockEarlyDiscountIfPaidOnTime(Invoice invoice, decimal newPaidAmount)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (invoice.DiscountStatus == "ACTIVE"
            && invoice.Discount?.ConditionType == "EARLY_PAYMENT"
            && invoice.DiscountDeadline != null
            && today <= invoice.DiscountDeadline
            && newPaidAmount >= invoice.Amount)
        {
            invoice.DiscountStatus = "LOCKED";
        }
    }

    private static string CalculateInvoiceStatus(decimal paidAmount, decimal invoiceAmount)
    {
        if (paidAmount <= 0) return "UNPAID";
        return paidAmount < invoiceAmount ? "PARTIAL" : "PAID";
    }

    private static string AppendNote(string? original, string note)
    {
        if (string.IsNullOrWhiteSpace(original)) return note;
        var value = $"{original} | {note}";
        return value.Length <= 255 ? value : value[..255];
    }

    public static bool IsEnrollmentOwnedByStudent(int enrollmentId, int studentId)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments.Any(x => x.EnrollmentId == enrollmentId && x.StudentId == studentId);
    }

    public static bool HasOpenInvoiceForEnrollment(int enrollmentId, int? excludedInvoiceId = null)
    {
        using var context = new LanguageCenterContext();
        return context.Invoices.Any(x => x.EnrollmentId == enrollmentId
            && x.InvoiceId != excludedInvoiceId
            && (x.Status == "UNPAID" || x.Status == "PARTIAL"));
    }

    public static decimal GetPaidAmount(int invoiceId)
    {
        using var context = new LanguageCenterContext();
        return context.Payments.Where(x => x.InvoiceId == invoiceId)
            .Sum(x => (decimal?)x.AmountPaid) ?? 0;
    }

    public static bool HasPayments(int invoiceId) => GetPaidAmount(invoiceId) > 0;

    public static void CancelOpenByEnrollmentId(int enrollmentId)
    {
        using var context = new LanguageCenterContext();
        var openInvoices = context.Invoices
            .Where(i => i.EnrollmentId == enrollmentId
                && (i.Status == "UNPAID" || i.Status == "PARTIAL"))
            .ToList();

        foreach (var invoice in openInvoices)
        {
            invoice.Status = "CANCELLED";
            invoice.Note = (invoice.Note ?? "") + " | Cancelled due to enrollment drop";
        }
        context.SaveChanges();
    }
}
