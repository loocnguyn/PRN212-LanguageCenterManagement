using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class AdminDashboardControl : UserControl, IDashboardControl
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IPaymentService _paymentService = new PaymentService();

    public AdminDashboardControl(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => RefreshData();
    }

    public void RefreshData()
    {
        tbHeader.Text = $"Welcome, {_currentUser.Username}";

        try
        {
            var activeStudents = _studentService.GetAll().Count(s => s.Status == "ACTIVE");
            var activeTeachers = _teacherService.GetAll().Count(t => t.Status == "ACTIVE");

            var semester = _semesterService.GetActive();
            var ongoingClasses = 0;
            var phaseText = "No active semester";
            if (semester != null)
            {
                ongoingClasses = _classService.GetBySemesterId(semester.SemesterId)
                    .Count(c => c.Status == "ONGOING");
                var phase = _semesterService.GetActivePhase();
                phaseText = $"{semester.Name} — {phase}";
            }
            tbSubHeader.Text = phaseText;

            var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var monthlyRevenue = _paymentService
                .GetPaymentsByDateRange(firstDayOfMonth, lastDayOfMonth, null)
                .Sum(p => p.AmountPaid);

            var openInvoices = _invoiceService.Search("", null)
                .Count(i => i.Status is "UNPAID" or "PARTIAL");

            panelTiles.Children.Clear();
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Active Students", activeStudents.ToString(), "PeopleTeam24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Active Teachers", activeTeachers.ToString(), "PersonAccounts24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Ongoing Classes", ongoingClasses.ToString(), "ClipboardTask24", FindBrush("SecondaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Revenue This Month", monthlyRevenue.ToString("N0") + " đ", "ReceiptMoney24", FindBrush("SecondaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Open Invoices", openInvoices.ToString(), "DocumentBulletList24", FindBrush("DangerBrush"),
                openInvoices > 0 ? "Unpaid or partially paid" : null));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Brush FindBrush(string key) => (Brush)Application.Current.Resources[key];
}
