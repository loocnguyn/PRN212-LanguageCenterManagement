using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class InvoiceAddWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();

    private List<StudentItem> _students = new();
    private List<EnrollmentItem> _enrollments = new();

    public InvoiceAddWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadStudents();
    }

    private void LoadStudents()
    {
        try
        {
            _students = _studentService.GetAll()
                .OrderBy(x => x.StudentId)
                .Select(x => new StudentItem
                {
                    StudentId = x.StudentId,
                    DisplayText = $"{x.StudentId} - {x.FullName}"
                })
                .ToList();

            cboStudent.ItemsSource = _students;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách học viên: {ex.Message}", "Lỗi");
        }
    }

    private void CboStudent_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        txtAmount.Text = "";
        txtNote.Text = "";
        cboEnrollment.ItemsSource = null;

        if (cboStudent.SelectedItem is not StudentItem student) return;

        try
        {
            _enrollments = _enrollmentService.GetByStudentId(student.StudentId)
                .Where(x => x.Status == "ACTIVE")
                .OrderBy(x => x.EnrollmentId)
                .Select(x => new EnrollmentItem
                {
                    EnrollmentId = x.EnrollmentId,
                    StudentId = x.StudentId,
                    TuitionFee = x.Class.Course?.TuitionFee,
                    DisplayText = BuildEnrollmentDisplayText(x)
                })
                .ToList();

            cboEnrollment.ItemsSource = _enrollments;
            if (_enrollments.Count == 1)
                cboEnrollment.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách enrollment: {ex.Message}", "Lỗi");
        }
    }

    private void CboEnrollment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboEnrollment.SelectedItem is not EnrollmentItem enrollment) return;

        if (enrollment.TuitionFee.HasValue && enrollment.TuitionFee.Value > 0)
            txtAmount.Text = enrollment.TuitionFee.Value.ToString("0.##");

        if (string.IsNullOrWhiteSpace(txtNote.Text))
            txtNote.Text = $"Tuition fee for enrollment #{enrollment.EnrollmentId}";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (cboStudent.SelectedItem is not StudentItem student)
            {
                MessageBox.Show("Vui lòng chọn học viên.");
                return;
            }
            if (cboEnrollment.SelectedItem is not EnrollmentItem enrollment)
            {
                MessageBox.Show("Vui lòng chọn enrollment/lớp học.");
                return;
            }

            var studentId = student.StudentId;
            var enrollmentId = enrollment.EnrollmentId;

            if (!decimal.TryParse(txtAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Số tiền phải lớn hơn 0.");
                return;
            }
            if (dpDueDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày đến hạn.");
                return;
            }
            if (!_service.IsEnrollmentOwnedByStudent(enrollmentId, studentId))
            {
                MessageBox.Show("Enrollment không thuộc học viên đã chọn.",
                    "Dữ liệu không hợp lệ");
                return;
            }
            if (_service.HasOpenInvoiceForEnrollment(enrollmentId))
            {
                MessageBox.Show(
                    "Enrollment này đã có hóa đơn chưa thanh toán hoặc thanh toán một phần.",
                    "Dữ liệu không hợp lệ");
                return;
            }

            _service.Save(new Invoice
            {
                StudentId = studentId,
                EnrollmentId = enrollmentId,
                Amount = amount,
                DueDate = DateOnly.FromDateTime(dpDueDate.SelectedDate.Value),
                Note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim(),
                Status = "UNPAID",
                CreatedAt = DateTime.Now
            });
            MessageBox.Show("Thêm hóa đơn thành công.");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể thêm hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private static string BuildEnrollmentDisplayText(Enrollment enrollment)
    {
        var className = enrollment.Class?.Name ?? $"Class #{enrollment.ClassId}";
        var courseName = enrollment.Class?.Course?.Name;
        var fee = enrollment.Class?.Course?.TuitionFee;
        var courseText = string.IsNullOrWhiteSpace(courseName) ? "" : $" - {courseName}";
        var feeText = fee.HasValue ? $" - {fee.Value:N0} VND" : "";
        return $"{enrollment.EnrollmentId} - {className}{courseText}{feeText}";
    }

    private sealed class StudentItem
    {
        public int StudentId { get; init; }
        public string DisplayText { get; init; } = "";
    }

    private sealed class EnrollmentItem
    {
        public int EnrollmentId { get; init; }
        public int StudentId { get; init; }
        public decimal? TuitionFee { get; init; }
        public string DisplayText { get; init; } = "";
    }
}
