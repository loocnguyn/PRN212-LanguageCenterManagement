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
    private List<InvoiceDisplayItem> _baseItems = new();
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
            _baseItems = _service.Search(txtSearch.Text, status)
                .Select(ToDisplayItem)
                .ToList();
            RefreshAcademicFilterOptions();
            _items = _baseItems
                .Where(MatchesAdvancedFilter)
                .ToList();
            _currentPage = 1;
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();

    private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        => advancedPanel.Visibility = advancedPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Clear();
        cmbStatus.SelectedIndex = 0;
        cmbPaymentState.SelectedIndex = 0;
        cmbDiscountStatus.SelectedIndex = 0;
        cmbSemester.SelectedIndex = 0;
        cmbCourse.SelectedIndex = 0;
        cmbClass.SelectedIndex = 0;
        cmbTeacher.SelectedIndex = 0;
        dpDueFrom.SelectedDate = null;
        dpDueTo.SelectedDate = null;
        dpCreatedFrom.SelectedDate = null;
        dpCreatedTo.SelectedDate = null;
        txtAmountMin.Clear();
        txtRemainingMin.Clear();
        ApplyFilter();
    }

    private bool MatchesAdvancedFilter(InvoiceDisplayItem item)
    {
        if (!IsDateInRange(item.DueDate, dpDueFrom.SelectedDate, dpDueTo.SelectedDate))
            return false;
        if (!IsDateInRange(DateOnly.FromDateTime(item.CreatedAt), dpCreatedFrom.SelectedDate, dpCreatedTo.SelectedDate))
            return false;

        var paymentState = (cmbPaymentState.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (paymentState == "Has Debt" && item.RemainingAmount <= 0) return false;
        if (paymentState == "Paid Full" && item.RemainingAmount > 0) return false;
        if (paymentState == "No Payment" && item.PaidAmount > 0) return false;
        if (paymentState == "Has Payment" && item.PaidAmount <= 0) return false;

        var discountStatus = (cmbDiscountStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (!string.IsNullOrWhiteSpace(discountStatus) && discountStatus != "All"
            && item.DiscountStatus != discountStatus)
            return false;

        if (!MatchesMinValue(txtAmountMin.Text, item.Amount)) return false;
        if (!MatchesMinValue(txtRemainingMin.Text, item.RemainingAmount)) return false;
        if (!MatchesAcademicFilters(item)) return false;

        return true;
    }

    private void RefreshAcademicFilterOptions()
    {
        SetComboOptions(cmbSemester, _baseItems.Select(x => x.SemesterName));
        SetComboOptions(cmbCourse, _baseItems.Select(x => x.CourseName));
        SetComboOptions(cmbClass, _baseItems.Select(x => x.ClassName));
        SetComboOptions(cmbTeacher, _baseItems.Select(x => x.TeacherName));
    }

    private static void SetComboOptions(ComboBox combo, IEnumerable<string> values)
    {
        var current = combo.SelectedItem?.ToString() ?? "All";
        var items = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .Prepend("All")
            .ToList();
        combo.ItemsSource = items;
        combo.SelectedItem = items.Contains(current) ? current : "All";
    }

    private bool MatchesAcademicFilters(InvoiceDisplayItem item)
        => MatchesCombo(cmbSemester, item.SemesterName)
            && MatchesCombo(cmbCourse, item.CourseName)
            && MatchesCombo(cmbClass, item.ClassName)
            && MatchesCombo(cmbTeacher, item.TeacherName);

    private static bool MatchesCombo(ComboBox combo, string value)
    {
        var selected = combo.SelectedItem?.ToString();
        return string.IsNullOrWhiteSpace(selected) || selected == "All" || selected == value;
    }

    private static bool IsDateInRange(DateOnly? value, DateTime? from, DateTime? to)
    {
        if (!value.HasValue)
            return !from.HasValue && !to.HasValue;

        var date = value.Value.ToDateTime(TimeOnly.MinValue).Date;
        if (from.HasValue && date < from.Value.Date) return false;
        if (to.HasValue && date > to.Value.Date) return false;
        return true;
    }

    private static bool MatchesMinValue(string input, decimal value)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;
        return decimal.TryParse(input.Trim(), out var min) && value >= min;
    }

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

    private static InvoiceDisplayItem ToDisplayItem(Invoice invoice)
    {
        var paid = invoice.Payments.Sum(x => x.AmountPaid);
        return new InvoiceDisplayItem
        {
            InvoiceId = invoice.InvoiceId,
            StudentId = invoice.StudentId,
            StudentName = invoice.Student?.FullName ?? "",
            EnrollmentId = invoice.EnrollmentId,
            SemesterName = invoice.Enrollment?.Class?.Semester?.Name ?? "",
            CourseName = invoice.Enrollment?.Class?.Course?.Name ?? "",
            ClassName = invoice.Enrollment?.Class?.Name ?? "",
            TeacherName = invoice.Enrollment?.Class?.Teacher?.FullName ?? "",
            OriginalAmount = invoice.OriginalAmount > 0 ? invoice.OriginalAmount : invoice.Amount,
            DiscountText = invoice.Discount == null ? "" : $"{invoice.Discount.Code} - {invoice.Discount.Name}",
            DiscountAmount = invoice.DiscountAmount,
            Amount = invoice.Amount,
            PaidAmount = paid,
            RemainingAmount = Math.Max(0, invoice.Amount - paid),
            DiscountStatus = invoice.DiscountStatus,
            DiscountDeadline = invoice.DiscountDeadline,
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
        public string SemesterName { get; init; } = "";
        public string CourseName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public string TeacherName { get; init; } = "";
        public decimal OriginalAmount { get; init; }
        public string DiscountText { get; init; } = "";
        public decimal DiscountAmount { get; init; }
        public decimal Amount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string DiscountStatus { get; init; } = "";
        public DateOnly? DiscountDeadline { get; init; }
        public string Status { get; init; } = "";
        public DateOnly? DueDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? Note { get; init; }
    }
}
