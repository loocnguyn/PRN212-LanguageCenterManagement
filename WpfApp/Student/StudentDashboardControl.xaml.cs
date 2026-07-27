using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

// StudentDashboardControl — student home: hero + wallet/invoice/class tiles + upcoming sessions (RefreshData).
public partial class StudentDashboardControl : UserControl, IDashboardControl
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IWalletService _walletService = new WalletService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();

    public StudentDashboardControl(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => RefreshData();
    }

    public void RefreshData()
    {
        tbSubHeader.Text = "Your studies and finances at a glance";

        try
        {
            var student = _studentService.GetByUserId(_currentUser.Id);
            if (student == null)
            {
                MessageBox.Show("No student profile linked to this account.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            tbHeader.Text = $"Welcome back, {student.FullName}";

            var enrollments = _enrollmentService.GetByStudentId(student.StudentId);
            var classIds = enrollments.Select(e => e.ClassId).ToList();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcomingSessions = _sessionService.GetByClassIds(classIds)
                .Where(s => s.SessionDate >= today)
                .OrderBy(s => s.SessionDate)
                .Take(3)
                .ToList();

            var balance = _walletService.GetBalance(student.StudentId);

            var invoices = _invoiceService.GetAll().Where(i => i.StudentId == student.StudentId).ToList();
            var unpaidInvoices = invoices.Where(i => i.Status != "PAID").ToList();
            var totalOwed = unpaidInvoices.Sum(i => Math.Max(0, i.Amount - _invoiceService.GetPaidAmount(i.InvoiceId)));

            panelTiles.Children.Clear();
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Wallet Balance", balance.ToString("N0") + " đ", "Wallet24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Unpaid Invoices", unpaidInvoices.Count.ToString(), "MoneyDismiss24", FindBrush("DangerBrush"),
                unpaidInvoices.Count > 0 ? $"Total owed: {totalOwed:N0} đ" : null));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Enrolled Classes", enrollments.Count.ToString(), "Book24", FindBrush("PrimaryBrush")));

            lstUpcoming.ItemsSource = upcomingSessions.Count > 0
                ? upcomingSessions.Select(s => $"{s.SessionDate:dd/MM/yyyy} — {s.Class.Name} ({s.Topic ?? "No topic"})").ToList()
                : new List<string> { "No upcoming sessions." };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Brush FindBrush(string key) => (Brush)Application.Current.Resources[key];
}
