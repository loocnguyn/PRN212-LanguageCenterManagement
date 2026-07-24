using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  InvoiceManagementWindow — browse/manage all invoices.
//  CONTENTS:
//    1. Construction & ApplyFilter — load + keyword/status filter
//    2. Advanced filter            — toggle panel, clear
//    3. Filter matching            — combo options + per-row predicates
// ============================================================
public partial class InvoiceManagementWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private List<InvoiceDisplayItem> _items = new();
    private List<InvoiceDisplayItem> _baseItems = new();

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
            pager.Reset();
            UpdateStats();
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
        SetComboOptions(cmbSemester, _semesterService.GetAll().Select(x => x.Name));
        SetComboOptions(cmbCourse, _courseService.GetAll().Select(x => x.Name));
        SetComboOptions(cmbClass, _classService.GetAll().Select(x => x.Name));
        SetComboOptions(cmbTeacher, _teacherService.GetAll().Select(x => x.FullName));
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

    private void UpdateStats()
    {
        statCount.Text = _items.Count.ToString();
        statBilled.Text = $"{_items.Sum(x => x.Amount):N0} đ";
        statPaid.Text = $"{_items.Sum(x => x.PaidAmount):N0} đ";
        statRemaining.Text = $"{_items.Sum(x => x.RemainingAmount):N0} đ";
    }

    private void ShowCurrentPage()
    {
        dgInvoices.ItemsSource = pager.Slice(_items);
        emptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ShowCurrentPage();

    private void DgInvoices_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgInvoices.SelectedItem is InvoiceDisplayItem) BtnEdit_Click(sender, e);
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
            TeacherName = invoice.Enrollment?.Class?.PrimaryTeacher?.FullName ?? "",
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
