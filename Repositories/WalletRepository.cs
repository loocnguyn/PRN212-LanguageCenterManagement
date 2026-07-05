using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class WalletRepository : IWalletRepository
{
    public decimal GetBalance(int studentId) => WalletTransactionDAO.GetBalance(studentId);

    public List<WalletTransaction> GetByStudentId(int studentId)
        => WalletTransactionDAO.GetByStudentId(studentId);

    public WalletTransaction? GetByMomoOrderId(string momoOrderId)
        => WalletTransactionDAO.GetByMomoOrderId(momoOrderId);

    public WalletTransaction CreatePendingTopUp(int studentId, decimal amount, string momoOrderId)
        => WalletTransactionDAO.CreatePendingTopUp(studentId, amount, momoOrderId);

    public bool CompleteTopUp(string momoOrderId) => WalletTransactionDAO.CompleteTopUp(momoOrderId);

    public void FailTopUp(string momoOrderId) => WalletTransactionDAO.FailTopUp(momoOrderId);

    public void PayInvoiceFromWallet(int studentId, int invoiceId)
        => WalletTransactionDAO.PayInvoiceFromWallet(studentId, invoiceId);
}
