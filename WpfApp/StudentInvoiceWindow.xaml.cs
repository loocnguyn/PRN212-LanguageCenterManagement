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

    private void RefreshBalance()
        => tbBalance.Text = $"Số dư ví: {_walletService.GetBalance(_studentId):N0} đ";

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
            MessageBox.Show("Vui lòng chọn một hóa đơn cần thanh toán.", "Thông báo");
            return;
        }
        if (selected.Status == "PAID")
        {
            MessageBox.Show("Hóa đơn này đã được thanh toán.", "Thông báo");
            return;
        }

        try
        {
            _walletService.PayInvoiceFromWallet(_studentId, selected.InvoiceId);
            MessageBox.Show("Thanh toán học phí thành công!", "Thành công");
            LoadInvoices();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể thanh toán: {ex.Message}", "Lỗi");
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
