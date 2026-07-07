using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class InvoiceManagementWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private const int PageSize = 10;
    private List<InvoiceDisplayItem> _items = new();
    private int _currentPage = 1;

    public InvoiceManagementWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData() => ApplyFilter();

    private void ApplyFilter()
    {
        try
        {
            var status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _items = _service.Search(txtSearch.Text, status)
                .Select(ToDisplayItem).ToList();
            _currentPage = 1;
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ShowCurrentPage()
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(_items.Count / (double)PageSize));
        _currentPage = Math.Clamp(_currentPage, 1, totalPages);
        dgInvoices.ItemsSource = _items.Skip((_currentPage - 1) * PageSize)
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

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var window = new InvoiceAddWindow { Owner = this };
        if (window.ShowDialog() == true)
            LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgInvoices.SelectedItem is not InvoiceDisplayItem item)
        {
            MessageBox.Show("Vui lòng chọn hóa đơn.");
            return;
        }

        var window = new InvoiceEditWindow(item.InvoiceId) { Owner = this };
        if (window.ShowDialog() == true)
            LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgInvoices.SelectedItem is not InvoiceDisplayItem item)
        {
            MessageBox.Show("Vui lòng chọn hóa đơn.");
            return;
        }
        try
        {
            if (_service.HasPayments(item.InvoiceId))
            {
                MessageBox.Show("Không thể xóa hóa đơn đã có thanh toán.");
                return;
            }
            var confirm = MessageBox.Show($"Xóa hóa đơn #{item.InvoiceId}?", "Xác nhận",
                MessageBoxButton.YesNo);
            if (confirm != MessageBoxResult.Yes) return;
            _service.Delete(item.InvoiceId);
            MessageBox.Show("Xóa hóa đơn thành công.");
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể xóa hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private static InvoiceDisplayItem ToDisplayItem(Invoice invoice)
    {
        var paid = invoice.Payments.Sum(x => x.AmountPaid);
        return new InvoiceDisplayItem
        {
            InvoiceId = invoice.InvoiceId,
            StudentId = invoice.StudentId,
            StudentName = invoice.Student?.FullName ?? "",
            EnrollmentId = invoice.EnrollmentId,
            Amount = invoice.Amount,
            PaidAmount = paid,
            RemainingAmount = Math.Max(0, invoice.Amount - paid),
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            CreatedAt = invoice.CreatedAt,
            Note = invoice.Note
        };
    }

    private sealed class InvoiceDisplayItem
    {
        public int InvoiceId { get; init; }
        public int StudentId { get; init; }
        public string StudentName { get; init; } = "";
        public int? EnrollmentId { get; init; }
        public decimal Amount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string Status { get; init; } = "";
        public DateOnly? DueDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? Note { get; init; }
    }
}
