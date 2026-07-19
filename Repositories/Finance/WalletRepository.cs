using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// WalletRepository — thin pass-through from the service layer to WalletDAO.

public class WalletRepository : IWalletRepository
{
    public decimal GetBalance(int studentId) => WalletTransactionDAO.GetBalance(studentId);

    public List<WalletTransaction> GetByStudentId(int studentId)
        => WalletTransactionDAO.GetByStudentId(studentId);

    public WalletTransaction? GetByProviderOrderId(string providerOrderId)
        => WalletTransactionDAO.GetByProviderOrderId(providerOrderId);

    public WalletTransaction CreatePendingTopUp(int studentId, decimal amount, string providerOrderId)
        => WalletTransactionDAO.CreatePendingTopUp(studentId, amount, providerOrderId);

    public bool CompleteTopUp(string providerOrderId) => WalletTransactionDAO.CompleteTopUp(providerOrderId);

    public void FailTopUp(string providerOrderId) => WalletTransactionDAO.FailTopUp(providerOrderId);

    public void PayInvoiceFromWallet(int studentId, int invoiceId)
        => WalletTransactionDAO.PayInvoiceFromWallet(studentId, invoiceId);
}
