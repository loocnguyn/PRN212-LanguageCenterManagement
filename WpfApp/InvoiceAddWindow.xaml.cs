using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class InvoiceAddWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();

    public InvoiceAddWindow() => InitializeComponent();

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(txtStudentId.Text, out var studentId) || studentId <= 0)
            { MessageBox.Show("Student ID phải là số nguyên dương."); return; }
            if (!int.TryParse(txtEnrollmentId.Text, out var enrollmentId) || enrollmentId <= 0)
            { MessageBox.Show("Enrollment ID phải là số nguyên dương."); return; }
            if (!decimal.TryParse(txtAmount.Text, out var amount) || amount <= 0)
            { MessageBox.Show("Số tiền phải lớn hơn 0."); return; }
            if (dpDueDate.SelectedDate == null)
            { MessageBox.Show("Vui lòng chọn ngày đến hạn."); return; }
            if (!_service.IsEnrollmentOwnedByStudent(enrollmentId, studentId))
            {
                MessageBox.Show("Enrollment không thuộc học viên đã nhập.",
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
}
