using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Microsoft.Win32;
using Services;

namespace WpfApp;

// ============================================================
//  RevenueReportWindow — payments over a date range, with filters.
//  CONTENTS:
//    1. Fields & construction   — base vs filtered payment lists
//    2. GenerateReport          — pull payments, total, populate grid
//    3. Quick ranges            — this/last month, this quarter, clear
//    4. Advanced filter         — combo options + row matching
// ============================================================
public partial class RevenueReportWindow : Window
{
    private readonly IPaymentService _service = new PaymentService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IStaffService _staffService = new StaffService();
    private readonly IExcelExportService _excelExportService = new ExcelExportService();
    private List<PaymentDisplayItem> _baseItems = new();
    private List<PaymentDisplayItem> _items = new();

    public RevenueReportWindow()
    {
        InitializeComponent();
        ClearDateRange();
        RefreshAdvancedFilterOptions();
        GenerateReport();
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e) => GenerateReport();

    private void GenerateReport()
    {
        if (dpFrom.SelectedDate.HasValue && dpTo.SelectedDate.HasValue
            && dpFrom.SelectedDate.Value.Date > dpTo.SelectedDate.Value.Date)
        {
            MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            return;
        }

        try
        {
            var method = (cmbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var payments = _service.GetPaymentsByDateRange(
                dpFrom.SelectedDate,
                dpTo.SelectedDate,
                method);

            _baseItems = payments.Select(ToDisplayItem).ToList();
            RefreshAdvancedFilterOptions();
            _items = _baseItems.Where(MatchesAdvancedFilter).ToList();

            pager.Reset();
            BindPage();
            lblTotalRevenue.Text = FormatMoney(_items.Sum(x => x.AmountPaid));
            lblTotalPayments.Text = _items.Count.ToString();
            lblCashTotal.Text = FormatMoney(SumByMethod(_items, "Cash"));
            lblTransferTotal.Text = FormatMoney(SumByMethod(_items, "Transfer"));
            lblCardTotal.Text = FormatMoney(SumByMethod(_items, "Card"));
            lblWalletTotal.Text = FormatMoney(SumByMethod(_items, "Wallet"));

            if (_items.Count == 0)
            {
                MessageBox.Show(
                    "Không có thanh toán nào phù hợp với điều kiện tìm kiếm.",
                    "Không có dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo báo cáo doanh thu: {GetFullMessage(ex)}", "Lỗi");
        }
    }

    private void BindPage()
    {
        dgPayments.ItemsSource = pager.Slice(_items);
        emptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void BtnThisMonth_Click(object sender, RoutedEventArgs e)
    {
        SetThisMonth();
        GenerateReport();
    }

    private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        => advancedPanel.Visibility = advancedPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

    private void BtnLastMonth_Click(object sender, RoutedEventArgs e)
    {
        var firstThisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var firstLastMonth = firstThisMonth.AddMonths(-1);
        dpFrom.SelectedDate = firstLastMonth;
        dpTo.SelectedDate = firstThisMonth.AddDays(-1);
        GenerateReport();
    }

    private void BtnThisQuarter_Click(object sender, RoutedEventArgs e)
    {
        var quarterStartMonth = ((DateTime.Today.Month - 1) / 3) * 3 + 1;
        var quarterStart = new DateTime(DateTime.Today.Year, quarterStartMonth, 1);
        dpFrom.SelectedDate = quarterStart;
        dpTo.SelectedDate = quarterStart.AddMonths(3).AddDays(-1);
        GenerateReport();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        ClearDateRange();
        cmbMethod.SelectedIndex = 0;
        cmbSemester.SelectedIndex = 0;
        cmbCourse.SelectedIndex = 0;
        cmbClass.SelectedIndex = 0;
        cmbTeacher.SelectedIndex = 0;
        cmbStaff.SelectedIndex = 0;
        txtStudentSearch.Clear();
        txtAmountMin.Clear();
        txtAmountMax.Clear();
        GenerateReport();
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show(
                "Không có dữ liệu để xuất file.",
                "Export Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Lưu báo cáo doanh thu",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"RevenueReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var headers = new[]
            {
                "Payment ID",
                "Invoice ID",
                "Student ID",
                "Student Name",
                "Semester",
                "Course",
                "Class",
                "Teacher",
                "Amount Paid",
                "Payment Method",
                "Paid At",
                "Invoice Status",
                "Staff ID",
                "Staff Name",
                "Note"
            };

            var rows = _items.Select(x => new object?[]
            {
                x.PaymentId,
                x.InvoiceId,
                x.StudentId,
                x.StudentName,
                x.SemesterName,
                x.CourseName,
                x.ClassName,
                x.TeacherName,
                x.AmountPaid,
                x.PaymentMethod,
                x.PaidAt,
                x.InvoiceStatus,
                x.StaffId,
                x.StaffName,
                x.Note
            });

            _excelExportService.ExportToExcel(dialog.FileName, "Revenue Report", headers, rows);
            MessageBox.Show(
                $"Đã xuất {_items.Count} dòng báo cáo doanh thu ra file:\n{dialog.FileName}",
                "Export Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể xuất file Excel: {GetFullMessage(ex)}",
                "Lỗi",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ClearDateRange()
    {
        dpFrom.SelectedDate = null;
        dpTo.SelectedDate = null;
    }

    private void SetThisMonth()
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dpFrom.SelectedDate = firstDay;
        dpTo.SelectedDate = firstDay.AddMonths(1).AddDays(-1);
    }

    private void RefreshAdvancedFilterOptions()
    {
        SetComboOptions(cmbSemester, _semesterService.GetAll().Select(x => x.Name));
        SetComboOptions(cmbCourse, _courseService.GetAll().Select(x => x.Name));
        SetComboOptions(cmbClass, _classService.GetAll().Select(x => x.Name));
        SetComboOptions(cmbTeacher, _teacherService.GetAll().Select(x => x.FullName));
        SetComboOptions(cmbStaff, _staffService.GetAll().Select(x => x.FullName));
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

    private bool MatchesAdvancedFilter(PaymentDisplayItem item)
    {
        if (!MatchesCombo(cmbSemester, item.SemesterName)) return false;
        if (!MatchesCombo(cmbCourse, item.CourseName)) return false;
        if (!MatchesCombo(cmbClass, item.ClassName)) return false;
        if (!MatchesCombo(cmbTeacher, item.TeacherName)) return false;
        if (!MatchesCombo(cmbStaff, item.StaffName)) return false;

        var studentSearch = txtStudentSearch.Text.Trim();
        if (!string.IsNullOrWhiteSpace(studentSearch))
        {
            var isNumber = int.TryParse(studentSearch, out var studentId);
            if (!(isNumber && item.StudentId == studentId)
                && !item.StudentName.Contains(studentSearch, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!MatchesMinValue(txtAmountMin.Text, item.AmountPaid)) return false;
        if (!MatchesMaxValue(txtAmountMax.Text, item.AmountPaid)) return false;
        return true;
    }

    private static bool MatchesCombo(ComboBox combo, string value)
    {
        var selected = combo.SelectedItem?.ToString();
        return string.IsNullOrWhiteSpace(selected) || selected == "All" || selected == value;
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

    private static decimal SumByMethod(IEnumerable<PaymentDisplayItem> payments, string method) =>
        payments.Where(x => x.PaymentMethod == method).Sum(x => x.AmountPaid);

    private static string FormatMoney(decimal amount) => amount.ToString("N0");

    private static string GetFullMessage(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current != null; current = current.InnerException)
            messages.Add(current.Message);
        return string.Join(Environment.NewLine, messages);
    }

    private static PaymentDisplayItem ToDisplayItem(Payment payment) => new()
    {
        PaymentId = payment.PaymentId,
        InvoiceId = payment.InvoiceId,
        StudentId = payment.Invoice.StudentId,
        StudentName = payment.Invoice.Student?.FullName ?? "",
        SemesterName = payment.Invoice.Enrollment?.Class?.Semester?.Name ?? "",
        CourseName = payment.Invoice.Enrollment?.Class?.Course?.Name ?? "",
        ClassName = payment.Invoice.Enrollment?.Class?.Name ?? "",
        TeacherName = payment.Invoice.Enrollment?.Class?.PrimaryTeacher?.FullName ?? "",
        AmountPaid = payment.AmountPaid,
        PaymentMethod = payment.PaymentMethod,
        PaidAt = payment.PaidAt,
        InvoiceStatus = payment.Invoice.Status,
        StaffId = payment.StaffId,
        StaffName = payment.Staff?.FullName ?? "",
        Note = payment.Note
    };

    private sealed class PaymentDisplayItem
    {
        public int PaymentId { get; init; }
        public int InvoiceId { get; init; }
        public int StudentId { get; init; }
        public string StudentName { get; init; } = "";
        public string SemesterName { get; init; } = "";
        public string CourseName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public string TeacherName { get; init; } = "";
        public decimal AmountPaid { get; init; }
        public string PaymentMethod { get; init; } = "";
        public DateTime PaidAt { get; init; }
        public string InvoiceStatus { get; init; } = "";
        public int? StaffId { get; init; }
        public string StaffName { get; init; } = "";
        public string? Note { get; init; }
    }
}
