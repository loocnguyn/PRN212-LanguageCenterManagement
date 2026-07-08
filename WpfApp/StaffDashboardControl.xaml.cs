using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class StaffDashboardControl : UserControl, IDashboardControl
{
    private readonly User _currentUser;
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IPaymentService _paymentService = new PaymentService();

    public StaffDashboardControl(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => RefreshData();
    }

    public void RefreshData()
    {
        tbHeader.Text = $"Welcome, {_currentUser.Username}";
        tbSubHeader.Text = "Enrollment and finance overview";

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var newEnrollments7d = _enrollmentService.GetAll()
                .Count(e => e.EnrolledDate >= today.AddDays(-7));

            var invoices = _invoiceService.Search("", null);
            var overdue = invoices.Where(i =>
                    i.Status != "PAID" &&
                    i.DueDate.HasValue && i.DueDate.Value < today)
                .ToList();
            var stillOwed = invoices
                .Where(i => i.Status != "PAID")
                .Sum(i => Math.Max(0, i.Amount - _invoiceService.GetPaidAmount(i.InvoiceId)));

            var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var collectedThisMonth = _paymentService
                .GetPaymentsByDateRange(firstDayOfMonth, lastDayOfMonth, null)
                .Sum(p => p.AmountPaid);

            panelTiles.Children.Clear();
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "New Enrollments (7d)", newEnrollments7d.ToString(), "PersonAdd24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Overdue Invoices", overdue.Count.ToString(), "MoneyDismiss24", FindBrush("DangerBrush"),
                overdue.Count > 0 ? "Past due date, not fully paid" : null));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Collected This Month", collectedThisMonth.ToString("N0") + " đ", "ReceiptMoney24", FindBrush("SecondaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Still Owed (All)", stillOwed.ToString("N0") + " đ", "Wallet24", FindBrush("DangerBrush")));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Brush FindBrush(string key) => (Brush)Application.Current.Resources[key];
}
