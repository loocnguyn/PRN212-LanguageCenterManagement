using BusinessObjects;

namespace DataAccessObjects;

public class WalletTransactionDAO
{
    public static List<WalletTransaction> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.WalletTransactions.ToList();
    }

    public static WalletTransaction? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.WalletTransactions.FirstOrDefault(x => x.TransactionId == id);
    }

    public static List<WalletTransaction> GetByStudentId(int studentId)
    {
        using var context = new LanguageCenterContext();
        return context.WalletTransactions
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    public static WalletTransaction? GetByMomoOrderId(string momoOrderId)
    {
        using var context = new LanguageCenterContext();
        return context.WalletTransactions.FirstOrDefault(x => x.MomoOrderId == momoOrderId);
    }

    public static void Save(WalletTransaction entity)
    {
        using var context = new LanguageCenterContext();
        context.WalletTransactions.Add(entity);
        context.SaveChanges();
    }

    public static void Update(WalletTransaction entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.WalletTransactions.Find(entity.TransactionId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }
}
