using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// WalletHistoryWindow — read-only, paged list of the student's wallet transactions.
public partial class WalletHistoryWindow : Window
{
    private readonly IWalletService _walletService = new WalletService();
    private readonly int _studentId;
    private List<WalletTransaction> _all = new();

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
            _all = _walletService.GetHistory(_studentId);
            pager.Reset();
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load wallet history: {ex.Message}", "Error");
        }
    }

    private void BindPage() => dgTransactions.ItemsSource = pager.Slice(_all);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void BtnReload_Click(object sender, RoutedEventArgs e) => LoadHistory();
}
