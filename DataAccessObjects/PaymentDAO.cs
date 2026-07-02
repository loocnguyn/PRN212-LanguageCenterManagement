using System.Data;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;

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
        EnsureReceiptCode(entity);
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

    public static void RecordPayment(Payment payment)
    {
        using var context = new LanguageCenterContext();
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var invoice = context.Invoices
                .Include(x => x.Payments)
                .SingleOrDefault(x => x.InvoiceId == payment.InvoiceId)
                ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");

            var paidAmount = invoice.Payments.Sum(x => x.AmountPaid);
            var remainingAmount = invoice.Amount - paidAmount;

            if (invoice.Status == "PAID" || remainingAmount <= 0)
                throw new InvalidOperationException("Hóa đơn đã được thanh toán đủ.");
            if (payment.AmountPaid <= 0)
                throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");
            if (payment.AmountPaid > remainingAmount)
                throw new InvalidOperationException("Số tiền thanh toán không được lớn hơn số tiền còn lại.");

            payment.PaidAt = DateTime.Now;
            EnsureReceiptCode(payment);
            context.Payments.Add(payment);

            var newPaidAmount = paidAmount + payment.AmountPaid;
            invoice.Status = newPaidAmount <= 0
                ? "UNPAID"
                : newPaidAmount < invoice.Amount ? "PARTIAL" : "PAID";

            context.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    private static void EnsureReceiptCode(Payment payment)
    {
        if (!string.IsNullOrWhiteSpace(payment.ReceiptCode)) return;

        payment.ReceiptCode = $"RCP-{Guid.NewGuid():N}";
    }
}
