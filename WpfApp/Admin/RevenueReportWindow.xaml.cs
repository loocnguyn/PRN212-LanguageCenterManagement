using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class RevenueReportWindow : Window
{
    private readonly IPaymentService _service = new PaymentService();
    private List<PaymentDisplayItem> _baseItems = new();
    private List<PaymentDisplayItem> _items = new();

    public RevenueReportWindow()
    {
        InitializeComponent();
        SetThisMonth();
        GenerateReport();
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e) => GenerateReport();

    private void GenerateReport()
    {
        if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
        {
            MessageBox.Show("Vui lòng chọn đầy đủ ngày bắt đầu và ngày kết thúc.");
            return;
        }
        if (dpFrom.SelectedDate.Value.Date > dpTo.SelectedDate.Value.Date)
        {
            MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            return;
        }

        try
        {
            var method = (cmbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var payments = _service.GetPaymentsByDateRange(
                dpFrom.SelectedDate.Value,
                dpTo.SelectedDate.Value,
                method);

            _baseItems = payments.Select(ToDisplayItem).ToList();
            RefreshAdvancedFilterOptions();
            _items = _baseItems.Where(MatchesAdvancedFilter).ToList();

            dgPayments.ItemsSource = _items;
            lblTotalRevenue.Text = FormatMoney(_items.Sum(x => x.AmountPaid));
            lblTotalPayments.Text = _items.Count.ToString();
            lblCashTotal.Text = FormatMoney(SumByMethod(_items, "Cash"));
            lblTransferTotal.Text = FormatMoney(SumByMethod(_items, "Transfer"));
            lblCardTotal.Text = FormatMoney(SumByMethod(_items, "Card"));
            lblWalletTotal.Text = FormatMoney(SumByMethod(_items, "Wallet"));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo báo cáo doanh thu: {GetFullMessage(ex)}", "Lỗi");
        }
    }

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
        dpFrom.SelectedDate = null;
        dpTo.SelectedDate = null;
        cmbMethod.SelectedIndex = 0;
        cmbSemester.SelectedIndex = 0;
        cmbCourse.SelectedIndex = 0;
        cmbClass.SelectedIndex = 0;
        cmbTeacher.SelectedIndex = 0;
        cmbStaff.SelectedIndex = 0;
        txtStudentSearch.Clear();
        txtAmountMin.Clear();
        txtAmountMax.Clear();
        _baseItems.Clear();
        _items.Clear();
        dgPayments.ItemsSource = null;
        lblTotalRevenue.Text = "";
        lblTotalPayments.Text = "";
        lblCashTotal.Text = "";
        lblTransferTotal.Text = "";
        lblCardTotal.Text = "";
        lblWalletTotal.Text = "";
    }

    private void SetThisMonth()
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dpFrom.SelectedDate = firstDay;
        dpTo.SelectedDate = firstDay.AddMonths(1).AddDays(-1);
    }

    private void RefreshAdvancedFilterOptions()
    {
        SetComboOptions(cmbSemester, _baseItems.Select(x => x.SemesterName));
        SetComboOptions(cmbCourse, _baseItems.Select(x => x.CourseName));
        SetComboOptions(cmbClass, _baseItems.Select(x => x.ClassName));
        SetComboOptions(cmbTeacher, _baseItems.Select(x => x.TeacherName));
        SetComboOptions(cmbStaff, _baseItems.Select(x => x.StaffName));
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
        TeacherName = payment.Invoice.Enrollment?.Class?.Teacher?.FullName ?? "",
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
