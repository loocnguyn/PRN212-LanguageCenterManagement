using System.Text;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class PaymentWindow : Window
{
    private readonly IPaymentService _payService = new PaymentService();
    private readonly IInvoiceService _invService = new InvoiceService();
    private readonly IStaffService _staffService = new StaffService();
    private const int PageSize = 10;
    private List<OutstandingInvoiceItem> _items = new();
    private int _currentPage = 1;

    public PaymentWindow()
    {
        InitializeComponent();
        LoadStaff();
        LoadInvoices();
    }

    private void LoadStaff()
    {
        try
        {
            cmbStaff.ItemsSource = _staffService.GetAll()
                .OrderBy(x => x.StaffId)
                .Select(x => new StaffOption
                {
                    StaffId = x.StaffId,
                    DisplayText = $"{x.StaffId} - {x.FullName}"
                })
                .ToList();
            if (cmbStaff.Items.Count > 0)
                cmbStaff.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách nhân viên:\n{GetExceptionMessages(ex)}", "Lỗi");
        }
    }

    private void LoadInvoices()
    {
        try
        {
            var keyword = txtSearch.Text.Trim();
            _items = _invService.Search(keyword, "All")
                .Where(x => x.Status is "UNPAID" or "PARTIAL")
                .Select(ToDisplayItem)
                .ToList();
            _currentPage = 1;
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách hóa đơn:\n{GetExceptionMessages(ex)}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadInvoices();

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

    private void DgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgInvoices.SelectedItem is not OutstandingInvoiceItem item) return;
        txtInvoiceId.Text = item.InvoiceId.ToString();
        txtRemaining.Text = item.RemainingAmount.ToString("0.##");
        txtAmountPaid.Text = "";
        txtAmountPaid.Focus();
    }

    private void BtnPay_Click(object sender, RoutedEventArgs e)
    {
        if (dgInvoices.SelectedItem is not OutstandingInvoiceItem item)
        {
            MessageBox.Show("Vui lòng chọn hóa đơn cần thanh toán.");
            return;
        }

        try
        {
            if (!decimal.TryParse(txtAmountPaid.Text, out var amountPaid) || amountPaid <= 0)
            {
                MessageBox.Show("Số tiền thanh toán phải lớn hơn 0.");
                return;
            }
            if (amountPaid > item.RemainingAmount)
            {
                MessageBox.Show("Số tiền thanh toán không được lớn hơn số tiền còn lại.");
                return;
            }
            if (cmbStaff.SelectedValue is not int staffId)
            {
                MessageBox.Show("Vui lòng chọn nhân viên ghi nhận thanh toán.");
                return;
            }

            var method = (cmbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(method))
            {
                MessageBox.Show("Vui lòng chọn phương thức thanh toán.");
                return;
            }

            _payService.RecordPayment(new Payment
            {
                InvoiceId = item.InvoiceId,
                StaffId = staffId,
                AmountPaid = amountPaid,
                PaymentMethod = method,
                Note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim()
            });

            MessageBox.Show("Ghi nhận thanh toán thành công.");
            ClearForm();
            LoadInvoices();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể ghi nhận thanh toán:\n{GetExceptionMessages(ex)}", "Lỗi");
        }
    }

    private static string GetExceptionMessages(Exception exception)
    {
        var messages = new StringBuilder();
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (messages.Length > 0) messages.AppendLine();
            messages.Append(current.Message);
        }
        return messages.ToString();
    }

    private void ClearForm()
    {
        dgInvoices.SelectedItem = null;
        txtInvoiceId.Text = "";
        txtRemaining.Text = "";
        txtAmountPaid.Text = "";
        txtNote.Text = "";
        cmbMethod.SelectedIndex = 0;
    }

    private static OutstandingInvoiceItem ToDisplayItem(Invoice invoice)
    {
        var paidAmount = invoice.Payments.Sum(x => x.AmountPaid);
        return new OutstandingInvoiceItem
        {
            InvoiceId = invoice.InvoiceId,
            StudentId = invoice.StudentId,
            StudentName = invoice.Student?.FullName ?? "",
            Amount = invoice.Amount,
            PaidAmount = paidAmount,
            RemainingAmount = Math.Max(0, invoice.Amount - paidAmount),
            Status = invoice.Status,
            DueDate = invoice.DueDate
        };
    }

    private sealed class StaffOption
    {
        public int StaffId { get; init; }
        public string DisplayText { get; init; } = "";
    }

    private sealed class OutstandingInvoiceItem
    {
        public int InvoiceId { get; init; }
        public int StudentId { get; init; }
        public string StudentName { get; init; } = "";
        public decimal Amount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string Status { get; init; } = "";
        public DateOnly? DueDate { get; init; }
    }
}
