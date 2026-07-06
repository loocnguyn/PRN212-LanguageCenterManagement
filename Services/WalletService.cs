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
        if (amount != decimal.Truncate(amount))
            throw new InvalidOperationException("Số tiền nạp phải là số nguyên VND.");

        var orderId = $"TOPUP-{studentId}-{DateTime.Now:yyyyMMddHHmmssfff}";
        var orderInfo = $"Nap tien vi hoc sinh #{studentId}";

        // Persist PENDING first so every MoMo order can be tracked even if the API call fails.
        _repo.CreatePendingTopUp(studentId, amount, orderId);
        try
        {
            var result = await _moMoService.CreatePaymentAsync(orderId, amount, orderInfo);
            if (!result.Success || string.IsNullOrEmpty(result.PayUrl))
                throw new InvalidOperationException(
                    $"Không thể tạo giao dịch MoMo: {result.Message} (code {result.ResultCode})");

            return (orderId, result.PayUrl);
        }
        catch
        {
            _repo.FailTopUp(orderId);
            throw;
        }
    }

    public async Task<bool> ConfirmTopUpAsync(string orderId)
    {
        var status = await _moMoService.QueryTransactionStatusAsync(orderId);

        if (status.IsSuccess)
        {
            var pending = _repo.GetByMomoOrderId(orderId)
                ?? throw new InvalidOperationException($"Không tìm thấy giao dịch '{orderId}'.");
            if (status.Amount != (long)pending.Amount)
            {
                _repo.FailTopUp(orderId);
                throw new InvalidOperationException("Số tiền MoMo xác nhận không khớp giao dịch.");
            }
            return _repo.CompleteTopUp(orderId);
        }

        if (!status.IsPending)
        {
            _repo.FailTopUp(orderId);
            throw new InvalidOperationException(
                $"Giao dịch MoMo thất bại: {status.Message} (code {status.ResultCode})");
        }

        return false;
    }

    public void FailTopUp(string orderId) => _repo.FailTopUp(orderId);

    public void PayInvoiceFromWallet(int studentId, int invoiceId)
        => _repo.PayInvoiceFromWallet(studentId, invoiceId);
}
