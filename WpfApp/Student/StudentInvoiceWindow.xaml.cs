using System.Linq;
using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  StudentInvoiceWindow — the student's invoices + wallet actions.
//  CONTENTS:
//    1. Construction & load  — invoices + wallet balance
//    2. Wallet actions       — top up, history, pay from wallet
// ============================================================
public partial class StudentInvoiceWindow : Window
{
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IStudentService _studentService = new StudentService();
    private readonly IWalletService _walletService = new WalletService();

    private readonly User _currentUser;
    private int _studentId;
    private List<InvoiceDisplayItem> _invoices = new();

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

            tbStudentName.Text = student.FullName;
            RefreshBalance();

            _invoices = _invoiceService.Search(_studentId.ToString(), null)
                .Where(x => x.StudentId == _studentId)
                .Where(x => x.Status is "UNPAID" or "PARTIAL")
                .Select(x =>
                {
                    var paidAmount = _invoiceService.GetPaidAmount(x.InvoiceId);
                    return new InvoiceDisplayItem
                    {
                        InvoiceId = x.InvoiceId,
                        ClassName = x.Enrollment?.Class?.Name ?? "Tuition",
                        OriginalAmount = x.OriginalAmount > 0 ? x.OriginalAmount : x.Amount,
                        DiscountText = x.Discount == null
                            ? ""
                            : $"{x.Discount.Name} — saves {x.DiscountAmount:N0} đ",
                        DiscountAmount = x.DiscountAmount,
                        Amount = x.Amount,
                        PaidAmount = paidAmount,
                        RemainingAmount = Math.Max(0, x.Amount - paidAmount),
                        DiscountStatus = x.DiscountStatus,
                        DiscountDeadline = x.DiscountDeadline,
                        DueDate = x.DueDate,
                        Status = x.Status,
                        Note = x.Note
                    };
                })
                .Where(x => x.RemainingAmount > 0)
                .OrderBy(x => x.DueDate)
                .ToList();

            UpdateSummary();
            pager.Reset();
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading invoices: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BindPage()
    {
        dgInvoices.ItemsSource = pager.Slice(_invoices);
        emptyState.Visibility = _invoices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    /// <summary>The four tiles above the grid — the totals a student actually asks about.</summary>
    private void UpdateSummary()
    {
        statOutstanding.Text = $"{_invoices.Sum(i => i.RemainingAmount):N0} đ";
        statPaid.Text = $"{_invoices.Sum(i => i.PaidAmount):N0} đ";
        statSaved.Text = $"{_invoices.Sum(i => i.DiscountAmount):N0} đ";
        statOverdue.Text = _invoices.Count(i => i.IsOverdue).ToString();
    }

    private void RefreshBalance()
        => tbBalance.Text = $"{_walletService.GetBalance(_studentId):N0} đ";

    private void BtnTopUp_Click(object sender, RoutedEventArgs e)
    {
        if (_studentId == 0) return;

        var topUpWindow = new TopUpWalletWindow(_studentId);
        topUpWindow.ShowDialog();
        if (topUpWindow.TopUpCompleted)
        {
            RefreshBalance();
            LoadInvoices();
        }
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
        if (selected.Status == "PAID" || selected.RemainingAmount <= 0)
        {
            MessageBox.Show("This invoice has already been paid.", "Info");
            return;
        }

        var balance = _walletService.GetBalance(_studentId);
        if (balance < selected.RemainingAmount)
        {
            MessageBox.Show(
                $"Insufficient wallet balance. Remaining amount is {selected.RemainingAmount:N0} VND, but wallet balance is {balance:N0} VND.",
                "Info");
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

        /// <summary>The class this invoice is for — what the student recognises it by.</summary>
        public string ClassName { get; init; } = "";

        public decimal OriginalAmount { get; init; }
        public string DiscountText { get; init; } = "";
        public decimal DiscountAmount { get; init; }
        public decimal Amount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string DiscountStatus { get; init; } = "";
        public DateOnly? DiscountDeadline { get; init; }
        public DateOnly? DueDate { get; init; }
        public string Status { get; init; } = "";
        public string? Note { get; init; }

        // ---- Display-only helpers the grid binds to ----
        public bool HasDiscount => DiscountAmount > 0;

        /// <summary>Past its due date and not settled — the row turns red on this.</summary>
        public bool IsOverdue => DueDate.HasValue
                                 && DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
                                 && RemainingAmount > 0;

        public string AmountText => $"{Amount:N0} đ";
        public string PaidText => $"{PaidAmount:N0} đ";
        public string RemainingText => $"{RemainingAmount:N0} đ";
        public string DueText => DueDate?.ToString("dd/MM/yyyy") ?? "—";
    }
}
