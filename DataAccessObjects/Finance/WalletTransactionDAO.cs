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
            throw new InvalidOperationException("Top-up amount must be greater than 0.");

        var transaction = new WalletTransaction
        {
            StudentId = studentId,
            Amount = amount,
            TransactionType = "TOP_UP",
            ProviderOrderId = providerOrderId,
            Description = "Wallet top-up via ZaloPay",
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
                throw new InvalidOperationException($"Top-up transaction '{providerOrderId}' not found.");

            if (walletTransaction.Status == "COMPLETED")
            {
                dbTransaction.Commit();
                return true;
            }
            if (walletTransaction.Status != "PENDING")
                throw new InvalidOperationException(
                    $"Transaction '{providerOrderId}' is already {walletTransaction.Status} and cannot be completed.");

            var student = context.Students.Find(walletTransaction.StudentId)
                ?? throw new InvalidOperationException("Student not found.");

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
                ?? throw new InvalidOperationException("Student not found.");

            var invoice = context.Invoices
                .Include(x => x.Discount)
                .Include(x => x.Payments)
                .SingleOrDefault(x => x.InvoiceId == invoiceId && x.StudentId == studentId)
                ?? throw new InvalidOperationException("Invoice not found for this student.");

            InvoiceDAO.ApplyExpiredEarlyDiscounts(context);
            context.Entry(invoice).Reload();
            context.Entry(invoice).Reference(x => x.Discount).Load();
            context.Entry(invoice).Collection(x => x.Payments).Load();

            var paidAmount = invoice.Payments.Sum(x => x.AmountPaid);
            var remainingAmount = Math.Max(0, invoice.Amount - paidAmount);

            if (invoice.Status == "PAID" || remainingAmount <= 0)
                throw new InvalidOperationException("This invoice has already been paid in full.");
            if (student.Balance < remainingAmount)
                throw new InvalidOperationException("Insufficient wallet balance to pay this invoice.");

            student.Balance -= remainingAmount;

            context.Payments.Add(new Payment
            {
                InvoiceId = invoiceId,
                AmountPaid = remainingAmount,
                PaymentMethod = "Wallet",
                PaidAt = DateTime.Now,
                ReceiptCode = $"RCP-{Guid.NewGuid():N}",
                Note = "Tuition payment via wallet"
            });

            invoice.Status = "PAID";
            InvoiceDAO.LockEarlyDiscountIfPaidOnTime(invoice, paidAmount + remainingAmount);

            context.WalletTransactions.Add(new WalletTransaction
            {
                StudentId = studentId,
                Amount = remainingAmount,
                TransactionType = "PAYMENT",
                Description = $"Payment for invoice #{invoiceId}",
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
