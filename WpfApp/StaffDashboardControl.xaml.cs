using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class StaffDashboardControl : UserControl, IDashboardControl
{
    private readonly User _currentUser;
    private readonly IStaffService _staffService = new StaffService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IPaymentService _paymentService = new PaymentService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly IClassService _classService = new ClassService();

    public StaffDashboardControl(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => RefreshData();
    }

    public void RefreshData()
    {
        try
        {
            var staff = _staffService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            var department = staff?.Department;
            tbHeader.Text = $"Staff — {staff?.FullName ?? _currentUser.Username}";

            panelTiles.Children.Clear();

            if (department != "Academic Affairs")
                LoadFinanceTiles();

            if (department != "Finance")
                LoadAcademicTiles();

            tbSubHeader.Text = department switch
            {
                "Finance" => "Tuition collection overview",
                "Academic Affairs" => "Class and semester setup overview",
                _ => "Enrollment, finance, and class setup overview"
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadFinanceTiles()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

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

        panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
            "Overdue Invoices", overdue.Count.ToString(), "MoneyDismiss24", FindBrush("DangerBrush"),
            overdue.Count > 0 ? "Past due date, not fully paid" : null));
        panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
            "Collected This Month", collectedThisMonth.ToString("N0") + " đ", "ReceiptMoney24", FindBrush("SecondaryBrush")));
        panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
            "Still Owed (All)", stillOwed.ToString("N0") + " đ", "Wallet24", FindBrush("DangerBrush")));
    }

    private void LoadAcademicTiles()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var newEnrollments7d = _enrollmentService.GetAll()
            .Count(e => e.EnrolledDate >= today.AddDays(-7));

        var semester = _semesterService.GetActive();
        var ongoingClasses = 0;
        string semesterText = "No active semester";
        if (semester != null)
        {
            ongoingClasses = _classService.GetBySemesterId(semester.SemesterId)
                .Count(c => c.Status == "ONGOING");
            var phase = _semesterService.GetActivePhase();
            semesterText = $"{semester.Name} — {phase}";
        }

        panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
            "New Enrollments (7d)", newEnrollments7d.ToString(), "PersonAdd24", FindBrush("PrimaryBrush")));
        panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
            "Ongoing Classes", ongoingClasses.ToString(), "ClipboardTask24", FindBrush("SecondaryBrush"), semesterText));
    }

    private static Brush FindBrush(string key) => (Brush)Application.Current.Resources[key];
}
