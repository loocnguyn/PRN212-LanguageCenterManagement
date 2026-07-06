using System.Linq;
using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class StudentInvoiceWindow : Window
{
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IStudentService _studentService = new StudentService();
    private readonly IWalletService _walletService = new WalletService();

    private readonly User _currentUser;
    private int _studentId;

    public StudentInvoiceWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadInvoices();
    }

    private void LoadInvoices()
    {
        try
        {
            var student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null)
            {
                tbStudentName.Text = "No student profile linked to this account.";
                return;
            }
            _studentId = student.StudentId;

            tbStudentName.Text = $"Student: {student.FullName}";
            RefreshBalance();

            var invoices = _invoiceService.Search(_studentId.ToString(), null)
                .Where(x => x.StudentId == _studentId)
                .Where(x => x.Status is "UNPAID" or "PARTIAL")
                .Select(x => new InvoiceDisplayItem
                {
                    InvoiceId = x.InvoiceId,
                    Amount = x.Amount,
                    PaidAmount = _invoiceService.GetPaidAmount(x.InvoiceId),
                    DueDate = x.DueDate,
                    Status = x.Status,
                    Note = x.Note
                })
                .ToList();

            dgInvoices.ItemsSource = invoices;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading invoices: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshBalance()
        => tbBalance.Text = $"Wallet balance: {_walletService.GetBalance(_studentId):N0} VND";

    private void BtnTopUp_Click(object sender, RoutedEventArgs e)
    {
        if (_studentId == 0) return;

        var topUpWindow = new TopUpWalletWindow(_studentId);
        topUpWindow.ShowDialog();
        if (topUpWindow.TopUpCompleted)
            RefreshBalance();
    }

    private void BtnHistory_Click(object sender, RoutedEventArgs e)
    {
        if (_studentId == 0) return;
        new WalletHistoryWindow(_studentId).ShowDialog();
    }

    private void BtnPayFromWallet_Click(object sender, RoutedEventArgs e)
    {
        if (_studentId == 0) return;

        if (dgInvoices.SelectedItem is not InvoiceDisplayItem selected)
        {
            MessageBox.Show("Please select an invoice to pay.", "Info");
            return;
        }
        if (selected.Status == "PAID")
        {
            MessageBox.Show("This invoice has already been paid.", "Info");
            return;
        }

        try
        {
            _walletService.PayInvoiceFromWallet(_studentId, selected.InvoiceId);
            MessageBox.Show("Tuition payment successful!", "Success");
            LoadInvoices();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not process payment: {ex.Message}", "Error");
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e) => LoadInvoices();

    private sealed class InvoiceDisplayItem
    {
        public int InvoiceId { get; init; }
        public decimal Amount { get; init; }
        public decimal PaidAmount { get; init; }
        public DateOnly? DueDate { get; init; }
        public string Status { get; init; } = "";
        public string? Note { get; init; }
    }
}
