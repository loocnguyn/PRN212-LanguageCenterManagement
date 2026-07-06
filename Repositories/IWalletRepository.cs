using BusinessObjects;

namespace Repositories;

public interface IWalletRepository
{
    decimal GetBalance(int studentId);
    List<WalletTransaction> GetByStudentId(int studentId);
    WalletTransaction? GetByProviderOrderId(string providerOrderId);
    WalletTransaction CreatePendingTopUp(int studentId, decimal amount, string providerOrderId);
    bool CompleteTopUp(string providerOrderId);
    void FailTopUp(string providerOrderId);
    void PayInvoiceFromWallet(int studentId, int invoiceId);
}
