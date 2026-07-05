using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class RevenueReportWindow : Window
{
    private readonly IPaymentService _service = new PaymentService();

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

            dgPayments.ItemsSource = payments.Select(ToDisplayItem).ToList();
            lblTotalRevenue.Text = FormatMoney(payments.Sum(x => x.AmountPaid));
            lblTotalPayments.Text = payments.Count.ToString();
            lblCashTotal.Text = FormatMoney(SumByMethod(payments, "Cash"));
            lblTransferTotal.Text = FormatMoney(SumByMethod(payments, "Transfer"));
            lblCardTotal.Text = FormatMoney(SumByMethod(payments, "Card"));
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
        dgPayments.ItemsSource = null;
        lblTotalRevenue.Text = "";
        lblTotalPayments.Text = "";
        lblCashTotal.Text = "";
        lblTransferTotal.Text = "";
        lblCardTotal.Text = "";
    }

    private void SetThisMonth()
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dpFrom.SelectedDate = firstDay;
        dpTo.SelectedDate = firstDay.AddMonths(1).AddDays(-1);
    }

    private static decimal SumByMethod(IEnumerable<Payment> payments, string method) =>
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
        AmountPaid = payment.AmountPaid,
        PaymentMethod = payment.PaymentMethod,
        PaidAt = payment.PaidAt,
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
        public decimal AmountPaid { get; init; }
        public string PaymentMethod { get; init; } = "";
        public DateTime PaidAt { get; init; }
        public int? StaffId { get; init; }
        public string StaffName { get; init; } = "";
        public string? Note { get; init; }
    }
}
