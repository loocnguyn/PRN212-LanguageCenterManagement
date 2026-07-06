using System.Data;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class WalletTransactionDAO
{
    public static decimal GetBalance(int studentId)
    {
        using var context = new LanguageCenterContext();
        return context.Students
            .Where(x => x.StudentId == studentId)
            .Select(x => x.Balance)
            .FirstOrDefault();
    }

    public static WalletTransaction CreatePendingTopUp(int studentId, decimal amount, string providerOrderId)
    {
        using var context = new LanguageCenterContext();
        if (amount <= 0)
            throw new InvalidOperationException("Số tiền nạp phải lớn hơn 0.");

        var transaction = new WalletTransaction
        {
            StudentId = studentId,
            Amount = amount,
            TransactionType = "TOP_UP",
            ProviderOrderId = providerOrderId,
            Description = "Nạp tiền vào ví qua ZaloPay",
            Status = "PENDING",
            CreatedAt = DateTime.Now
        };
        context.WalletTransactions.Add(transaction);
        context.SaveChanges();
        return transaction;
    }

    public static bool CompleteTopUp(string providerOrderId)
    {
        using var context = new LanguageCenterContext();
        using var dbTransaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var walletTransaction = context.WalletTransactions
                .FirstOrDefault(x => x.ProviderOrderId == providerOrderId);
            if (walletTransaction == null)
                throw new InvalidOperationException($"Không tìm thấy giao dịch nạp tiền '{providerOrderId}'.");

            if (walletTransaction.Status == "COMPLETED")
            {
                dbTransaction.Commit();
                return true;
            }
            if (walletTransaction.Status != "PENDING")
                throw new InvalidOperationException(
                    $"Giao dịch '{providerOrderId}' đã ở trạng thái {walletTransaction.Status}, không thể hoàn tất.");

            var student = context.Students.Find(walletTransaction.StudentId)
                ?? throw new InvalidOperationException("Không tìm thấy học sinh.");

            student.Balance += walletTransaction.Amount;
            walletTransaction.Status = "COMPLETED";

            context.SaveChanges();
            dbTransaction.Commit();
            return true;
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
    }

    public static void FailTopUp(string providerOrderId)
    {
        using var context = new LanguageCenterContext();
        var walletTransaction = context.WalletTransactions
            .FirstOrDefault(x => x.ProviderOrderId == providerOrderId);
        if (walletTransaction == null || walletTransaction.Status != "PENDING") return;

        walletTransaction.Status = "FAILED";
        context.SaveChanges();
    }

    public static void PayInvoiceFromWallet(int studentId, int invoiceId)
    {
        using var context = new LanguageCenterContext();
        using var dbTransaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var student = context.Students.Find(studentId)
                ?? throw new InvalidOperationException("Không tìm thấy học sinh.");

            var invoice = context.Invoices
                .Include(x => x.Payments)
                .SingleOrDefault(x => x.InvoiceId == invoiceId && x.StudentId == studentId)
                ?? throw new InvalidOperationException("Không tìm thấy hóa đơn của học sinh này.");

            var paidAmount = invoice.Payments.Sum(x => x.AmountPaid);
            var remainingAmount = invoice.Amount - paidAmount;

            if (invoice.Status == "PAID" || remainingAmount <= 0)
                throw new InvalidOperationException("Hóa đơn đã được thanh toán đủ.");
            if (student.Balance < remainingAmount)
                throw new InvalidOperationException("Số dư ví không đủ để thanh toán hóa đơn này.");

            student.Balance -= remainingAmount;

            context.Payments.Add(new Payment
            {
                InvoiceId = invoiceId,
                AmountPaid = remainingAmount,
                PaymentMethod = "Wallet",
                PaidAt = DateTime.Now,
                ReceiptCode = $"RCP-{Guid.NewGuid():N}",
                Note = "Thanh toán học phí bằng ví"
            });

            invoice.Status = "PAID";

            context.WalletTransactions.Add(new WalletTransaction
            {
                StudentId = studentId,
                Amount = remainingAmount,
                TransactionType = "PAYMENT",
                Description = $"Thanh toán hóa đơn #{invoiceId}",
                Status = "COMPLETED",
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();
            dbTransaction.Commit();
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
    }

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

    public static WalletTransaction? GetByProviderOrderId(string providerOrderId)
    {
        using var context = new LanguageCenterContext();
        return context.WalletTransactions.FirstOrDefault(x => x.ProviderOrderId == providerOrderId);
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
