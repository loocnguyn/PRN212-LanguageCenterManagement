using BusinessObjects;

namespace Services;

public interface IWalletService
{
    decimal GetBalance(int studentId);
    List<WalletTransaction> GetHistory(int studentId);
    Task<(string orderId, string payUrl)> StartTopUpAsync(int studentId, decimal amount);
    Task<bool> ConfirmTopUpAsync(string orderId);
    void FailTopUp(string orderId);
    void PayInvoiceFromWallet(int studentId, int invoiceId);
}
