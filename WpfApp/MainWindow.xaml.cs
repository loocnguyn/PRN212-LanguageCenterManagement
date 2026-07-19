using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class MainWindow : Window
{
    private readonly User _currentUser;
    private readonly IStaffService _staffService = new StaffService();
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
                menuAccounts.Visibility = Visibility.Visible;
                menuSemesters.Visibility = Visibility.Visible;
                menuCourses.Visibility  = Visibility.Visible;
                menuClasses.Visibility  = Visibility.Visible;
                menuReports.Visibility  = Visibility.Visible;
                menuDiscounts.Visibility = Visibility.Visible;
                // Admin is a superset of both Staff departments: academic setup (Students/Enrollment)
                // and finance (Debt List/Invoice/Payment) are both available on top of Admin's own menus.
                menuStudents.Visibility = Visibility.Visible;
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

    /// <summary>Staff without a Department set (e.g. legacy seeded accounts) get both menus
    /// so nobody is locked out; new accounts always require a Department (see AccountDetailWindow).</summary>
    private void ApplyStaffDepartmentVisibility()
    {
        var department = _staffService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id)?.Department;

        var showAcademic = department is null or "Academic Setup";
        var showFinance = department is null or "Finance";

        menuStudents.Visibility = showAcademic ? Visibility.Visible : Visibility.Collapsed;
        menuSemesters.Visibility = showAcademic ? Visibility.Visible : Visibility.Collapsed;
        menuCourses.Visibility = showAcademic ? Visibility.Visible : Visibility.Collapsed;
        menuClasses.Visibility = showAcademic ? Visibility.Visible : Visibility.Collapsed;
        menuFinance.Visibility = showFinance ? Visibility.Visible : Visibility.Collapsed;
        menuDiscounts.Visibility = Visibility.Collapsed;
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
    private void MenuSemesters_Click(object sender, RoutedEventArgs e)
        => new SemesterWindow().Show();
    private void MenuCourses_Click(object sender, RoutedEventArgs e)
        => new CourseManagementWindow().Show();
    private void MenuClassrooms_Click(object sender, RoutedEventArgs e)
        => new ClassroomManagementWindow().Show();
    private void MenuGradeTypeManagement_Click(object sender, RoutedEventArgs e)
        => new GradeTypeManagementWindow().Show();
    private void MenuClasses_Click(object sender, RoutedEventArgs e)
        => new ClassManagementWindow().Show();
    private void MenuRevenueReport_Click(object sender, RoutedEventArgs e)
        => new RevenueReportWindow().Show();
    private void MenuTuitionDiscounts_Click(object sender, RoutedEventArgs e)
        => new TuitionDiscountManagementWindow().Show();

    // STAFF
    private void MenuStudents_Click(object sender, RoutedEventArgs e)
        => new StudentManagementWindow().Show();
    private void MenuEnrollment_Click(object sender, RoutedEventArgs e)
        => new EnrollmentWindow().Show();
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
