using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class InvoiceEditWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private readonly Invoice _invoice;

    public InvoiceEditWindow(int invoiceId)
    {
        InitializeComponent();
        _invoice = _service.GetById(invoiceId)
            ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        LoadInvoice();
    }

    private void LoadInvoice()
    {
        var paid = _service.GetPaidAmount(_invoice.InvoiceId);
        var remaining = Math.Max(0, _invoice.Amount - paid);

        txtInvoiceId.Text = _invoice.InvoiceId.ToString();
        txtCreatedAt.Text = _invoice.CreatedAt.ToString("g");
        txtStudentId.Text = _invoice.StudentId.ToString();
        txtStudentName.Text = _invoice.Student?.FullName ?? "";
        txtEnrollmentId.Text = _invoice.EnrollmentId?.ToString() ?? "";
        txtStatus.Text = _invoice.Status;

        txtSemester.Text = _invoice.Enrollment?.Class?.Semester?.Name ?? "";
        txtCourse.Text = _invoice.Enrollment?.Class?.Course?.Name ?? "";
        txtClass.Text = _invoice.Enrollment?.Class?.Name ?? "";
        txtTeacher.Text = _invoice.Enrollment?.Class?.Teacher?.FullName ?? "";

        txtOriginalAmount.Text = (_invoice.OriginalAmount > 0 ? _invoice.OriginalAmount : _invoice.Amount).ToString("N0");
        txtDiscount.Text = _invoice.Discount == null ? "" : $"{_invoice.Discount.Code} - {_invoice.Discount.Name}";
        txtDiscountAmount.Text = _invoice.DiscountAmount.ToString("N0");
        txtDiscountStatus.Text = _invoice.DiscountStatus;
        txtPaidAmount.Text = paid.ToString("N0");
        txtRemainingAmount.Text = remaining.ToString("N0");

        txtAmount.Text = _invoice.Amount.ToString("0.##");
        dpDueDate.DisplayDateStart = DateTime.Today;
        dpDueDate.SelectedDate = _invoice.DueDate?.ToDateTime(TimeOnly.MinValue);
        txtNote.Text = _invoice.Note ?? "";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
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
            if (dpDueDate.SelectedDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Due date không được là ngày trong quá khứ.");
                return;
            }

            var paid = _service.GetPaidAmount(_invoice.InvoiceId);
            if (amount < paid)
            {
                MessageBox.Show(
                    "Số tiền hóa đơn không được nhỏ hơn tổng tiền đã thanh toán.",
                    "Dữ liệu không hợp lệ");
                return;
            }

            _invoice.Amount = amount;
            _invoice.DueDate = DateOnly.FromDateTime(dpDueDate.SelectedDate.Value);
            _invoice.Note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();
            _invoice.Status = paid <= 0 ? "UNPAID" : paid >= amount ? "PAID" : "PARTIAL";
            _service.Update(_invoice);
            MessageBox.Show("Cập nhật hóa đơn thành công.");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể cập nhật hóa đơn: {ex.Message}", "Lỗi");
        }
    }
}
