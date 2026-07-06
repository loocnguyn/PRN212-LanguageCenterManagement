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
        tbBalance.Text = $"Current balance: {_walletService.GetBalance(_studentId):N0} VND";
    }

    private async void BtnTopUp_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(txtAmount.Text.Trim(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var amount)
            || amount <= 0 || amount != decimal.Truncate(amount))
        {
            MessageBox.Show("Please enter a whole VND amount greater than 0.", "Error");
            return;
        }

        try
        {
            btnTopUp.IsEnabled = false;
            tbStatus.Text = "Creating ZaloPay transaction...";

            var (orderId, payUrl) = await _walletService.StartTopUpAsync(_studentId, amount);
            Process.Start(new ProcessStartInfo(payUrl) { UseShellExecute = true });

            _pendingOrderId = orderId;
            _pollStartedAt = DateTime.Now;
            tbStatus.Text = "Waiting for payment confirmation from ZaloPay...";

            _pollTimer = new DispatcherTimer { Interval = PollInterval };
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            btnTopUp.IsEnabled = true;
            MessageBox.Show($"Could not create top-up transaction: {ex.Message}", "Error");
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
                tbStatus.Text = "Timed out. The transaction has been marked as failed.";
            }
            catch (Exception ex)
            {
                tbStatus.Text = $"Timed out; could not update status: {ex.Message}";
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
                tbStatus.Text = "Top-up successful!";
                btnTopUp.IsEnabled = true;
                return;
            }
            _pollTimer?.Start();
        }
        catch (Exception ex)
        {
            StopPolling();
            btnTopUp.IsEnabled = true;
            tbStatus.Text = $"Error checking transaction: {ex.Message}";
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
