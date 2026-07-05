using BusinessObjects;
using Repositories;

namespace Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _repo = new WalletRepository();
    private readonly IMoMoService _moMoService = new MoMoService();

    public decimal GetBalance(int studentId) => _repo.GetBalance(studentId);

    public List<WalletTransaction> GetHistory(int studentId) => _repo.GetByStudentId(studentId);

    public async Task<(string orderId, string payUrl)> StartTopUpAsync(int studentId, decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Số tiền nạp phải lớn hơn 0.");

        var orderId = $"TOPUP-{studentId}-{DateTime.Now:yyyyMMddHHmmssfff}";
        var orderInfo = $"Nap tien vi hoc sinh #{studentId}";

        var result = await _moMoService.CreatePaymentAsync(orderId, amount, orderInfo);
        if (!result.Success || string.IsNullOrEmpty(result.PayUrl))
            throw new InvalidOperationException(
                $"Không thể tạo giao dịch MoMo: {result.Message} (code {result.ResultCode})");

        _repo.CreatePendingTopUp(studentId, amount, orderId);

        return (orderId, result.PayUrl);
    }

    public async Task<bool> ConfirmTopUpAsync(string orderId)
    {
        var status = await _moMoService.QueryTransactionStatusAsync(orderId);

        if (status.IsSuccess)
            return _repo.CompleteTopUp(orderId);

        if (!status.IsPending)
        {
            _repo.FailTopUp(orderId);
            return false;
        }

        return false;
    }

    public void PayInvoiceFromWallet(int studentId, int invoiceId)
        => _repo.PayInvoiceFromWallet(studentId, invoiceId);
}
