using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  MainWindow — the shell after login: top menu + hosted dashboard.
//  CONTENTS:
//    1. Fields & construction        — services, dashboard auto-refresh timer
//    2. OpenRoleDashboard            — swap in the right dashboard control
//    3. ApplyRoleVisibility          — show each role's top-level menus
//    4. ApplyStaffDepartmentVisibility — academic vs finance staff menus
//    5. LoadSemesterInfo             — status-bar/active-semester text
//    6. Menu click handlers          — grouped ADMIN / STAFF / TEACHER /
//                                      STUDENT / ALL; each opens a window
// ============================================================
public partial class MainWindow : Window
{
    // ---- 1. Fields & construction ------------------------------
    private readonly User _currentUser;
    private readonly IStaffService _staffService = new StaffService();
    private readonly IDepartmentService _departmentService = new DepartmentService();
    private readonly DispatcherTimer _dashboardRefreshTimer = new() { Interval = TimeSpan.FromSeconds(15) };

    public MainWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        ApplyRoleVisibility(currentUser.Role);
        LoadSemesterInfo();
        OpenRoleDashboard(currentUser.Role);

        _dashboardRefreshTimer.Tick += (_, _) => (dashboardHost.Content as IDashboardControl)?.RefreshData();
        _dashboardRefreshTimer.Start();
        Closed += (_, _) => _dashboardRefreshTimer.Stop();
    }

    private void OpenRoleDashboard(string role)
    {
        dashboardHost.Content = role switch
        {
            "ADMIN" => new AdminDashboardControl(_currentUser),
            "STAFF" => new StaffDashboardControl(_currentUser),
            "TEACHER" => new TeacherDashboardControl(_currentUser),
            "STUDENT" => new StudentDashboardControl(_currentUser),
            _ => null
        };
    }

    private void ApplyRoleVisibility(string role)
    {
        switch (role)
        {
            case "ADMIN":
                // Admin sees everything. Account sub-items (student/staff/departments/etc.)
                // default to Visible in XAML; Finance now also carries Revenue Report + Discounts.
                menuAccounts.Visibility = Visibility.Visible;
                menuSemesters.Visibility = Visibility.Visible;
                menuCourses.Visibility  = Visibility.Visible;
                menuClasses.Visibility  = Visibility.Visible;
                menuFinance.Visibility = Visibility.Visible;
                break;
            case "STAFF":
                ApplyStaffDepartmentVisibility();
                break;
            case "TEACHER":
                menuTeacherSchedule.Visibility = Visibility.Visible;
                menuTeacherAttendance.Visibility = Visibility.Visible;
                menuTeacherClasses.Visibility = Visibility.Visible;
                break;
            case "STUDENT":
                menuStudentSchedule.Visibility = Visibility.Visible;
                menuStudentAcademics.Visibility = Visibility.Visible;
                menuStudentFinance.Visibility = Visibility.Visible;
                menuStudentAssistant.Visibility = Visibility.Visible;
                break;
        }
    }

    /// <summary>
    /// Department names whose staff get the finance menus (invoices, payments, reports,
    /// discounts). Everything else counts as academic.
    ///
    /// Kept in code rather than as a column on Departments: it is a rule about this
    /// application's menus, not data about a department, and it changes when the menus
    /// change — not when an admin renames a row.
    /// </summary>
    private static readonly string[] FinanceDepartments = { "Finance" };

    /// <summary>Resolves the staff member's department to an access group and shows the matching
    /// menus. Staff whose department can't be resolved (e.g. blank) get both groups so
    /// nobody is locked out. Academic staff reach Student Management inside the Accounts menu but
    /// not the admin-only account tools.</summary>
    private void ApplyStaffDepartmentVisibility()
    {
        var deptName = _staffService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id)?.Department;

        var isFinance = deptName != null
            && FinanceDepartments.Contains(deptName, StringComparer.OrdinalIgnoreCase);

        var showAcademic = deptName == null || !isFinance;
        var showFinance = deptName == null || isFinance;

        if (showAcademic)
        {
            menuAccounts.Visibility = Visibility.Visible;
            // Only Student Management is relevant to academic staff; hide admin-only account tools.
            menuMiAccountManagement.Visibility = Visibility.Collapsed;
            menuMiTeacherManagement.Visibility = Visibility.Collapsed;
            menuMiStaffManagement.Visibility = Visibility.Collapsed;
            menuMiDepartments.Visibility = Visibility.Collapsed;
            menuAccountsSep.Visibility = Visibility.Collapsed;
            menuMiDeactivated.Visibility = Visibility.Collapsed;

            menuSemesters.Visibility = Visibility.Visible;
            menuCourses.Visibility = Visibility.Visible;
            menuClasses.Visibility = Visibility.Visible;
        }

        menuFinance.Visibility = showFinance ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadSemesterInfo()
    {
        try
        {
            var semesterService = new SemesterService();
            var active = semesterService.GetActive();
            if (active == null)
            {
                txtStatus.Text = "No active semester";
                txtWelcome.Text = $"Welcome, {_currentUser.Username} ({_currentUser.Role})";
                return;
            }

            Phase? phase = null;
            try { phase = semesterService.GetPhase(active); } catch { }

            var phaseText = phase.HasValue ? $" [{phase.Value}]" : "";
            txtWelcome.Text = $"Welcome, {_currentUser.Username} ({_currentUser.Role}) — {active.Name}{phaseText}";
            txtStatus.Text = $"Active Semester: {active.Name} | Phase: {phase}";
        }
        catch
        {
            txtWelcome.Text = $"Welcome, {_currentUser.Username} ({_currentUser.Role})";
            txtStatus.Text = "Ready";
        }
    }

    // ADMIN
    private void MenuClassSchedules_Click(object sender, RoutedEventArgs e)
        => new ClassScheduleManagementWindow().Show();
    private void MenuSlotSetting_Click(object sender, RoutedEventArgs e)
        => new SlotSettingWindow().Show();
    private void MenuAccountManagement_Click(object sender, RoutedEventArgs e)
        => new AccountManagementWindow(_currentUser).Show();
    private void MenuDeactivatedAccounts_Click(object sender, RoutedEventArgs e)
        => new DeactivatedAccountsWindow().Show();
    private void MenuStaffManagement_Click(object sender, RoutedEventArgs e)
        => new StaffManagementWindow().Show();
    private void MenuTeacherManagement_Click(object sender, RoutedEventArgs e)
        => new TeacherManagementWindow().Show();
    private void MenuDepartments_Click(object sender, RoutedEventArgs e)
        => new DepartmentManagementWindow().Show();
    private void MenuSemesters_Click(object sender, RoutedEventArgs e)
        => new SemesterWindow().Show();
    private void MenuCourses_Click(object sender, RoutedEventArgs e)
        => new CourseManagementWindow().Show();
    private void MenuCatalogue_Click(object sender, RoutedEventArgs e)
        => new CatalogueWindow().Show();
    private void MenuClassrooms_Click(object sender, RoutedEventArgs e)
        => new ClassroomManagementWindow().Show();
    private void MenuGradeTypeManagement_Click(object sender, RoutedEventArgs e)
        => new GradeTypeManagementWindow().Show();
    private void MenuRevenueReport_Click(object sender, RoutedEventArgs e)
        => new RevenueReportWindow().Show();
    private void MenuRewardReview_Click(object sender, RoutedEventArgs e)
        => new RewardReviewWindow().Show();
    private void MenuTuitionDiscounts_Click(object sender, RoutedEventArgs e)
        => new TuitionDiscountManagementWindow().Show();

    // STAFF
    private void MenuStudents_Click(object sender, RoutedEventArgs e)
        => new StudentManagementWindow().Show();
    private void MenuDebtList_Click(object sender, RoutedEventArgs e)
        => new DebtListWindow().Show();
    private void MenuInvoice_Click(object sender, RoutedEventArgs e)
        => new InvoiceManagementWindow().Show();
    private void MenuPayment_Click(object sender, RoutedEventArgs e)
        => new PaymentWindow(_currentUser).Show();

    // TEACHER — all 3 pass _currentUser for authorization
    private void MenuTeacherSchedule_Click(object sender, RoutedEventArgs e)
        => new TeacherScheduleWindow(_currentUser).Show();
    private void MenuAttendance_Click(object sender, RoutedEventArgs e)
        => new AttendanceWindow(_currentUser).Show();
    private void MenuGradeEntry_Click(object sender, RoutedEventArgs e)
        => new GradeEntryWindow(_currentUser).Show();
    private void MenuClassRoster_Click(object sender, RoutedEventArgs e)
        => new ClassRosterWindow(_currentUser).Show();
    private void MenuClassResults_Click(object sender, RoutedEventArgs e)
        => new ClassResultWindow(_currentUser).Show();

    // STUDENT
    private void MenuStudentSchedule_Click(object sender, RoutedEventArgs e)
        => new StudentScheduleWindow(_currentUser).Show();
    private void MenuMyClasses_Click(object sender, RoutedEventArgs e)
        => new MyClassesWindow(_currentUser).Show();
    private void MenuAttendanceHistory_Click(object sender, RoutedEventArgs e)
        => new AttendanceHistoryWindow(_currentUser).Show();
    private void MenuMyGrades_Click(object sender, RoutedEventArgs e)
        => new StudentGradeWindow(_currentUser).Show();
    private void MenuMyInvoices_Click(object sender, RoutedEventArgs e)
        => new StudentInvoiceWindow(_currentUser).Show();
    private void MenuAiAssistant_Click(object sender, RoutedEventArgs e)
        => new AiAssistantWindow(_currentUser).Show();

    // ALL roles
    private void MenuMyProfile_Click(object sender, RoutedEventArgs e)
        => new MyProfileWindow(_currentUser).Show();

    private void MenuLogout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        this.Close();
    }
}
