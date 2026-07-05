using BusinessObjects;

namespace Repositories;

public interface IWalletRepository
{
    decimal GetBalance(int studentId);
    List<WalletTransaction> GetByStudentId(int studentId);
    WalletTransaction? GetByMomoOrderId(string momoOrderId);
    WalletTransaction CreatePendingTopUp(int studentId, decimal amount, string momoOrderId);
    bool CompleteTopUp(string momoOrderId);
    void FailTopUp(string momoOrderId);
    void PayInvoiceFromWallet(int studentId, int invoiceId);
}
