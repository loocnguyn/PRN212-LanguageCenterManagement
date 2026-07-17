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
    private List<DebtItem> _baseItems = new();
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

            _baseItems = invoices
                .Select(ToDebtItem)
                .Where(x => x.Status != "PAID"
                    && (x.Status is "UNPAID" or "PARTIAL" || x.RemainingAmount > 0))
                .ToList();
            RefreshAcademicFilterOptions();
            _items = _baseItems.Where(MatchesAdvancedFilter).ToList();
            _currentPage = 1;
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách công nợ: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadData();

    private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        => advancedPanel.Visibility = advancedPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Clear();
        cmbStatus.SelectedIndex = 0;
        cmbDebtType.SelectedIndex = 0;
        cmbSemester.SelectedIndex = 0;
        cmbCourse.SelectedIndex = 0;
        cmbClass.SelectedIndex = 0;
        cmbTeacher.SelectedIndex = 0;
        dpDueFrom.SelectedDate = null;
        dpDueTo.SelectedDate = null;
        txtRemainingMin.Clear();
        txtRemainingMax.Clear();
        LoadData();
    }

    private bool MatchesAdvancedFilter(DebtItem item)
    {
        if (!IsDateInRange(item.DueDate, dpDueFrom.SelectedDate, dpDueTo.SelectedDate))
            return false;

        var debtType = (cmbDebtType.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var isOverdue = item.DueDate.HasValue
            && item.DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
            && item.RemainingAmount > 0;
        if (debtType == "Overdue" && !isOverdue) return false;
        if (debtType == "Not Overdue" && isOverdue) return false;

        if (!MatchesMinValue(txtRemainingMin.Text, item.RemainingAmount)) return false;
        if (!MatchesMaxValue(txtRemainingMax.Text, item.RemainingAmount)) return false;
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

    private bool MatchesAcademicFilters(DebtItem item)
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

    private static bool MatchesMaxValue(string input, decimal value)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;
        return decimal.TryParse(input.Trim(), out var max) && value <= max;
    }

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
            SemesterName = invoice.Enrollment?.Class?.Semester?.Name ?? "",
            CourseName = invoice.Enrollment?.Class?.Course?.Name ?? "",
            ClassName = invoice.Enrollment?.Class?.Name ?? "",
            TeacherName = invoice.Enrollment?.Class?.Teacher?.FullName ?? "",
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
        public string SemesterName { get; init; } = "";
        public string CourseName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public string TeacherName { get; init; } = "";
        public decimal TotalAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string Status { get; init; } = "";
        public DateOnly? DueDate { get; init; }
        public string? Note { get; init; }
    }
}
