using System.Windows;
using Services;

namespace WpfApp;

public partial class WalletHistoryWindow : Window
{
    private readonly IWalletService _walletService = new WalletService();
    private readonly int _studentId;

    public WalletHistoryWindow(int studentId)
    {
        InitializeComponent();
        _studentId = studentId;
        Loaded += (_, _) => LoadHistory();
    }

    private void LoadHistory()
    {
        tbBalance.Text = $"Số dư hiện tại: {_walletService.GetBalance(_studentId):N0} đ";
        dgTransactions.ItemsSource = _walletService.GetHistory(_studentId);
    }
}
