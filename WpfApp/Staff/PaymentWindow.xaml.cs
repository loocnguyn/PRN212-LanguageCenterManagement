using System.Text;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  PaymentWindow — record a tuition payment against an invoice.
//  CONTENTS:
//    1. Construction & staff   — resolve current staff (null for admin)
//    2. LoadInvoices           — outstanding invoices into the grid
//    3. Advanced filter        — toggle panel, clear, matching
//    4. Pay                    — record the payment (BtnPay_Click)
// ============================================================
public partial class PaymentWindow : Window
{
    private readonly IPaymentService _payService = new PaymentService();
    private readonly IInvoiceService _invService = new InvoiceService();
    private readonly IStaffService _staffService = new StaffService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly User _currentUser;
    private List<OutstandingInvoiceItem> _items = new();
    private List<OutstandingInvoiceItem> _baseItems = new();
    private int? _currentStaffId;

    public PaymentWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        LoadCurrentStaff();
        LoadInvoices();
    }

    private void LoadCurrentStaff()
    {
        try
        {
            var staff = _staffService.GetAll()
                .FirstOrDefault(x => x.UserId == _currentUser.Id);

            if (staff == null)
            {
                // Admins have no Staff profile; payments they record are stored with a null StaffId.
                _currentStaffId = null;
                txtStaff.Text = $"{_currentUser.Username} ({_currentUser.Role})";
                return;
            }

            _currentStaffId = staff.StaffId;
            txtStaff.Text = $"{staff.StaffId} - {staff.FullName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải thông tin nhân viên đang đăng nhập:\n{GetExceptionMessages(ex)}", "Lỗi");
        }
    }

    private void LoadInvoices()
    {
        try
        {
            var keyword = txtSearch.Text.Trim();
            var statusFilter = (cmbInvoiceStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _baseItems = _invService.Search(keyword, "All")
                .Where(x => x.Status is "UNPAID" or "PARTIAL")
                .Select(ToDisplayItem)
                .Where(x => statusFilter == "All" || x.Status == statusFilter)
                .ToList();
            RefreshAcademicFilterOptions();
            _items = _baseItems.Where(MatchesAdvancedFilter).ToList();
            pager.Reset();
            ShowCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách hóa đơn:\n{GetExceptionMessages(ex)}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadInvoices();

    private void BtnAdvanced_Click(object sender, RoutedEventArgs e)
        => advancedPanel.Visibility = advancedPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Clear();
        cmbInvoiceStatus.SelectedIndex = 0;
        cmbOverdue.SelectedIndex = 0;
        cmbSemester.SelectedIndex = 0;
        cmbCourse.SelectedIndex = 0;
        cmbClass.SelectedIndex = 0;
        cmbTeacher.SelectedIndex = 0;
        dpDueFrom.SelectedDate = null;
        dpDueTo.SelectedDate = null;
        txtRemainingMin.Clear();
        ClearForm();
        LoadInvoices();
    }

    private bool MatchesAdvancedFilter(OutstandingInvoiceItem item)
    {
        if (!IsDateInRange(item.DueDate, dpDueFrom.SelectedDate, dpDueTo.SelectedDate))
            return false;

        if (!MatchesMinValue(txtRemainingMin.Text, item.RemainingAmount))
            return false;

        var overdueFilter = (cmbOverdue.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var isOverdue = item.DueDate.HasValue
            && item.DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
            && item.RemainingAmount > 0;
        if (overdueFilter == "Overdue" && !isOverdue) return false;
        if (overdueFilter == "Not Overdue" && isOverdue) return false;
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

    private bool MatchesAcademicFilters(OutstandingInvoiceItem item)
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
        dgInvoices.ItemsSource = pager.Slice(_items);
        emptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ShowCurrentPage();

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
            var method = (cmbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(method))
            {
                MessageBox.Show("Vui lòng chọn phương thức thanh toán.");
                return;
            }

            _payService.RecordPayment(new Payment
            {
                InvoiceId = item.InvoiceId,
                StaffId = _currentStaffId,
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
            SemesterName = invoice.Enrollment?.Class?.Semester?.Name ?? "",
            CourseName = invoice.Enrollment?.Class?.Course?.Name ?? "",
            ClassName = invoice.Enrollment?.Class?.Name ?? "",
            TeacherName = invoice.Enrollment?.Class?.PrimaryTeacher?.FullName ?? "",
            Amount = invoice.Amount,
            PaidAmount = paidAmount,
            RemainingAmount = Math.Max(0, invoice.Amount - paidAmount),
            Status = invoice.Status,
            DueDate = invoice.DueDate
        };
    }

    private sealed class OutstandingInvoiceItem
    {
        public int InvoiceId { get; init; }
        public int StudentId { get; init; }
        public string StudentName { get; init; } = "";
        public string SemesterName { get; init; } = "";
        public string CourseName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public string TeacherName { get; init; } = "";
        public decimal Amount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal RemainingAmount { get; init; }
        public string Status { get; init; } = "";
        public DateOnly? DueDate { get; init; }
    }
}
