using BusinessObjects;

namespace DataAccessObjects;

public class PaymentDAO
{
    public static List<Payment> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Payments.ToList();
    }

    public static Payment? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Payments.FirstOrDefault(x => x.PaymentId == id);
    }

    public static void Save(Payment entity)
    {
        using var context = new LanguageCenterContext();
        context.Payments.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Payment entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Payments.Find(entity.PaymentId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Payments.Find(id);
        if (existing == null) return;
        context.Payments.Remove(existing);
        context.SaveChanges();
    }
}
