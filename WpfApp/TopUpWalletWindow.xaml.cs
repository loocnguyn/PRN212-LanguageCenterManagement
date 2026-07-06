using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Services;

namespace WpfApp;

public partial class TopUpWalletWindow : Window
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);

    private readonly IWalletService _walletService = new WalletService();
    private readonly int _studentId;

    private DispatcherTimer? _pollTimer;
    private string? _pendingOrderId;
    private DateTime _pollStartedAt;

    public bool TopUpCompleted { get; private set; }

    public TopUpWalletWindow(int studentId)
    {
        InitializeComponent();
        _studentId = studentId;
        RefreshBalance();
    }

    private void RefreshBalance()
    {
        tbBalance.Text = $"Số dư hiện tại: {_walletService.GetBalance(_studentId):N0} đ";
    }

    private async void BtnTopUp_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtAmount.Text.Trim(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var amount)
            || amount <= 0 || amount != decimal.Truncate(amount))
        {
            MessageBox.Show("Vui lòng nhập số tiền nguyên VND lớn hơn 0.", "Lỗi");
            return;
        }

        try
        {
            btnTopUp.IsEnabled = false;
            tbStatus.Text = "Đang tạo giao dịch MoMo...";

            var (orderId, payUrl) = await _walletService.StartTopUpAsync(_studentId, amount);
            Process.Start(new ProcessStartInfo(payUrl) { UseShellExecute = true });

            _pendingOrderId = orderId;
            _pollStartedAt = DateTime.Now;
            tbStatus.Text = "Đang chờ xác nhận thanh toán từ MoMo...";

            _pollTimer = new DispatcherTimer { Interval = PollInterval };
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            btnTopUp.IsEnabled = true;
            MessageBox.Show($"Không thể tạo giao dịch nạp tiền: {ex.Message}", "Lỗi");
        }
    }

    private async void PollTimer_Tick(object? sender, EventArgs e)
    {
        if (_pendingOrderId == null) return;

        if (DateTime.Now - _pollStartedAt > PollTimeout)
        {
            var timedOutOrderId = _pendingOrderId;
            StopPolling();
            try
            {
                _walletService.FailTopUp(timedOutOrderId);
                tbStatus.Text = "Hết thời gian chờ. Giao dịch đã được đánh dấu thất bại.";
            }
            catch (Exception ex)
            {
                tbStatus.Text = $"Hết thời gian chờ; không thể cập nhật trạng thái: {ex.Message}";
            }
            btnTopUp.IsEnabled = true;
            return;
        }

        try
        {
            _pollTimer?.Stop();
            var completed = await _walletService.ConfirmTopUpAsync(_pendingOrderId);
            if (completed)
            {
                StopPolling();
                TopUpCompleted = true;
                RefreshBalance();
                tbStatus.Text = "Nạp tiền thành công!";
                btnTopUp.IsEnabled = true;
                return;
            }
            _pollTimer?.Start();
        }
        catch (Exception ex)
        {
            StopPolling();
            btnTopUp.IsEnabled = true;
            tbStatus.Text = $"Lỗi khi kiểm tra giao dịch: {ex.Message}";
        }
    }

    private void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
        _pendingOrderId = null;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        StopPolling();
        Close();
    }
}
