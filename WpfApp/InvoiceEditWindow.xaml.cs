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
        txtStudentId.Text = _invoice.StudentId.ToString();
        txtEnrollmentId.Text = _invoice.EnrollmentId?.ToString() ?? "";
        txtAmount.Text = _invoice.Amount.ToString();
        dpDueDate.SelectedDate = _invoice.DueDate?.ToDateTime(TimeOnly.MinValue);
        txtNote.Text = _invoice.Note ?? "";
        txtStatus.Text = _invoice.Status;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(txtAmount.Text, out var amount) || amount <= 0)
            { MessageBox.Show("Số tiền phải lớn hơn 0."); return; }
            if (dpDueDate.SelectedDate == null)
            { MessageBox.Show("Vui lòng chọn ngày đến hạn."); return; }

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
