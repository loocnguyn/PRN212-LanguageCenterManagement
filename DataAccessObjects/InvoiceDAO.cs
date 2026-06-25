using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class InvoiceDAO
{
    public static List<Invoice> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Invoices.ToList();
    }

    public static Invoice? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Invoices.FirstOrDefault(x => x.InvoiceId == id);
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
        var existing = context.Invoices.Find(entity.InvoiceId)
            ?? throw new Exception("Invoice not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Invoices.Find(id)
            ?? throw new Exception("Invoice not found.");
        context.Invoices.Remove(existing);
        context.SaveChanges();
    }
}
