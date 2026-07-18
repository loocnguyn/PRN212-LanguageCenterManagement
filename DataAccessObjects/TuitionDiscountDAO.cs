using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class TuitionDiscountDAO
{
    public static List<TuitionDiscount> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.TuitionDiscounts
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToList();
    }

    public static List<TuitionDiscount> GetActive(DateOnly date)
    {
        using var context = new LanguageCenterContext();
        return context.TuitionDiscounts
            .AsNoTracking()
            .Where(x => x.IsActive
                && (x.StartDate == null || x.StartDate <= date)
                && (x.EndDate == null || x.EndDate >= date))
            .OrderBy(x => x.Code)
            .ToList();
    }

    public static List<TuitionDiscount> Search(string? keyword, string? status)
    {
        using var context = new LanguageCenterContext();
        var query = context.TuitionDiscounts.AsNoTracking().AsQueryable();

        keyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Code.Contains(keyword)
                || x.Name.Contains(keyword)
                || (x.Note != null && x.Note.Contains(keyword)));
        }

        if (status == "Active")
            query = query.Where(x => x.IsActive);
        else if (status == "Inactive")
            query = query.Where(x => !x.IsActive);

        return query.OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Code)
            .ToList();
    }

    public static TuitionDiscount? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.TuitionDiscounts.AsNoTracking()
            .FirstOrDefault(x => x.DiscountId == id);
    }

    public static void Save(TuitionDiscount entity)
    {
        using var context = new LanguageCenterContext();
        context.TuitionDiscounts.Add(entity);
        context.SaveChanges();
    }

    public static void Update(TuitionDiscount entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.TuitionDiscounts.Find(entity.DiscountId);
        if (existing == null)
            throw new InvalidOperationException("Discount not found.");

        existing.Code = entity.Code;
        existing.Name = entity.Name;
        existing.DiscountType = entity.DiscountType;
        existing.DiscountValue = entity.DiscountValue;
        existing.StartDate = entity.StartDate;
        existing.EndDate = entity.EndDate;
        existing.IsActive = entity.IsActive;
        existing.Note = entity.Note;
        existing.PaymentDeadlineDays = entity.PaymentDeadlineDays;
        existing.ConditionType = entity.ConditionType;
        context.SaveChanges();
    }

    public static void DeleteOrDeactivate(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.TuitionDiscounts.Find(id);
        if (existing == null)
            throw new InvalidOperationException("Discount not found.");

        var usedByInvoices = context.Invoices.Any(x => x.DiscountId == id);
        if (usedByInvoices)
        {
            existing.IsActive = false;
        }
        else
        {
            context.TuitionDiscounts.Remove(existing);
        }
        context.SaveChanges();
    }

    public static bool IsCodeTaken(string code, int? excludeId = null)
    {
        using var context = new LanguageCenterContext();
        code = code.Trim().ToUpper();
        return context.TuitionDiscounts.Any(x => x.Code == code
            && (!excludeId.HasValue || x.DiscountId != excludeId.Value));
    }
}
