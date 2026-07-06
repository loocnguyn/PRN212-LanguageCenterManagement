using BusinessObjects;
using Repositories;

namespace Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _repo = new WalletRepository();
    private readonly IZaloPayService _zaloPayService = new ZaloPayService();

    public decimal GetBalance(int studentId) => _repo.GetBalance(studentId);

    public List<WalletTransaction> GetHistory(int studentId) => _repo.GetByStudentId(studentId);

    public async Task<(string orderId, string payUrl)> StartTopUpAsync(int studentId, decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Số tiền nạp phải lớn hơn 0.");
        if (amount != decimal.Truncate(amount))
            throw new InvalidOperationException("Số tiền nạp phải là số nguyên VND.");

        // ZaloPay requires app_trans_id formatted as "yyMMdd_<unique>".
        var orderId = $"{DateTime.Now:yyMMdd}_{studentId}{DateTime.Now:HHmmssfff}";
        var description = $"Nap tien vi hoc sinh #{studentId}";

        // Persist PENDING first so every order can be tracked even if the API call fails.
        _repo.CreatePendingTopUp(studentId, amount, orderId);
        try
        {
            var result = await _zaloPayService.CreateOrderAsync(orderId, amount, description);
            if (!result.Success || string.IsNullOrEmpty(result.OrderUrl))
                throw new InvalidOperationException(
                    $"Không thể tạo giao dịch ZaloPay: {result.Message} (code {result.ReturnCode})");

            return (orderId, result.OrderUrl);
        }
        catch
        {
            _repo.FailTopUp(orderId);
            throw;
        }
    }

    public async Task<bool> ConfirmTopUpAsync(string orderId)
    {
        var status = await _zaloPayService.QueryOrderStatusAsync(orderId);

        if (status.IsSuccess)
        {
            var pending = _repo.GetByProviderOrderId(orderId)
                ?? throw new InvalidOperationException($"Không tìm thấy giao dịch '{orderId}'.");
            if (status.Amount != (long)pending.Amount)
            {
                _repo.FailTopUp(orderId);
                throw new InvalidOperationException("Số tiền ZaloPay xác nhận không khớp giao dịch.");
            }
            return _repo.CompleteTopUp(orderId);
        }

        if (!status.IsPending)
        {
            _repo.FailTopUp(orderId);
            throw new InvalidOperationException(
                $"Giao dịch ZaloPay thất bại: {status.Message} (code {status.ReturnCode})");
        }

        return false;
    }

    public void FailTopUp(string orderId) => _repo.FailTopUp(orderId);

    public void PayInvoiceFromWallet(int studentId, int invoiceId)
        => _repo.PayInvoiceFromWallet(studentId, invoiceId);
}
