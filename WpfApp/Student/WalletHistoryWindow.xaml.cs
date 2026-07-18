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
        try
        {
            tbBalance.Text = $"Current balance: {_walletService.GetBalance(_studentId):N0} VND";
            dgTransactions.ItemsSource = _walletService.GetHistory(_studentId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load wallet history: {ex.Message}", "Error");
        }
    }

    private void BtnReload_Click(object sender, RoutedEventArgs e) => LoadHistory();
}
