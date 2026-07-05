using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class MainWindow : Window
{
    private readonly User _currentUser;

    public MainWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        ApplyRoleVisibility(currentUser.Role);
        LoadSemesterInfo();
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
                break;
            case "STAFF":
                menuStudents.Visibility = Visibility.Visible;
                menuFinance.Visibility  = Visibility.Visible;
                break;
            case "TEACHER":
                menuMyClasses.Visibility = Visibility.Visible;
                break;
            case "STUDENT":
                menuMyInfo.Visibility = Visibility.Visible;
                break;
        }
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
    private void MenuClasses_Click(object sender, RoutedEventArgs e)
        => new ClassManagementWindow().Show();
    private void MenuRevenueReport_Click(object sender, RoutedEventArgs e)
        => new RevenueReportWindow().Show();

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
        => new PaymentWindow().Show();

    // TEACHER
    private void MenuTeacherSchedule_Click(object sender, RoutedEventArgs e)
        => new TeacherScheduleWindow(_currentUser).Show();
    private void MenuAttendance_Click(object sender, RoutedEventArgs e)
        => new AttendanceWindow().Show();
    private void MenuGradeEntry_Click(object sender, RoutedEventArgs e)
        => new GradeEntryWindow().Show();
    private void MenuClassRoster_Click(object sender, RoutedEventArgs e)
        => new ClassRosterWindow().Show();
    private void MenuClassResults_Click(object sender, RoutedEventArgs e)
        => new ClassResultWindow().Show();

    // STUDENT
    private void MenuStudentSchedule_Click(object sender, RoutedEventArgs e)
        => new StudentScheduleWindow(_currentUser).Show();
    private void MenuMyClasses_Click(object sender, RoutedEventArgs e)
        => new MyClassesWindow(_currentUser).Show();
    private void MenuAttendanceHistory_Click(object sender, RoutedEventArgs e)
        => new AttendanceHistoryWindow().Show();
    private void MenuMyGrades_Click(object sender, RoutedEventArgs e)
        => new StudentGradeWindow(_currentUser).Show();
    private void MenuMyInvoices_Click(object sender, RoutedEventArgs e)
        => new StudentInvoiceWindow().Show();

    private void MenuLogout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        this.Close();
    }
}
