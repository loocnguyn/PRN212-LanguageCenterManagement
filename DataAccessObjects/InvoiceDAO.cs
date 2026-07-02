using BusinessObjects;

using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class InvoiceDAO
{
    public static List<Invoice> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Invoices.Include(x => x.Student).Include(x => x.Enrollment)
            .Include(x => x.Payments).AsNoTracking().ToList();
    }

    public static Invoice? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Invoices.Include(x => x.Student).Include(x => x.Enrollment)
            .Include(x => x.Payments).AsNoTracking()
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
        var query = context.Invoices.Include(x => x.Student).Include(x => x.Enrollment)
            .Include(x => x.Payments).AsNoTracking().AsQueryable();
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
}
