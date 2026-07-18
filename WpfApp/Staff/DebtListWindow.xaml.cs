using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class DebtListWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private const int PageSize = 10;
    private List<DebtItem> _items = new();
    private int _currentPage = 1;

    public DebtListWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var invoices = _service.Search(txtSearch.Text, status);

            _items = invoices
                .Select(ToDebtItem)
                .Where(x => x.Status != "PAID"
                    && (x.Status is "UNPAID" or "PARTIAL" || x.RemainingAmount > 0))
                .ToList();
            _currentPage = 1;
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách công nợ: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadData();

    private void ShowCurrentPage()
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_items.Count / (double)PageSize));
        _currentPage = Math.Clamp(_currentPage, 1, totalPages);
        dgDebts.ItemsSource = _items.Skip((_currentPage - 1) * PageSize)
            .Take(PageSize).ToList();
        txtPageInfo.Text = $"Page {_currentPage}/{totalPages} ({_items.Count} items)";
        btnPrevious.IsEnabled = _currentPage > 1;
        btnNext.IsEnabled = _currentPage < totalPages;
    }

    private void BtnPrevious_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        ShowCurrentPage();
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_items.Count / (double)PageSize));
        if (_currentPage >= totalPages) return;
        _currentPage++;
        ShowCurrentPage();
    }

    private static DebtItem ToDebtItem(Invoice invoice)
    {
        var paidAmount = invoice.Payments.Sum(x => x.AmountPaid);
        return new DebtItem
        {
            InvoiceId = invoice.InvoiceId,
            StudentId = invoice.StudentId,
            StudentName = invoice.Student?.FullName ?? "",
            EnrollmentId = invoice.EnrollmentId,
            TotalAmount = invoice.Amount,
            PaidAmount = paidAmount,
            RemainingAmount = Math.Max(0, invoice.Amount - paidAmount),
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            Note = invoice.Note
        };
    }

    private sealed class DebtItem
    {
        public int InvoiceId { get; init; }
        public int StudentId { get; init; }
        public string StudentName { get; init; } = "";
        public int? EnrollmentId { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string Status { get; init; } = "";
        public DateOnly? DueDate { get; init; }
        public string? Note { get; init; }
    }
}
