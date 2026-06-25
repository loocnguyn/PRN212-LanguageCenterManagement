using System.Windows;
using System.Windows.Controls;
using BusinessObjects;

namespace WpfApp;

public partial class MainWindow : Window
{
    private readonly User _currentUser;

    public MainWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        txtWelcome.Text = $"Welcome, {currentUser.Username} ({currentUser.Role})";
        ApplyRoleVisibility(currentUser.Role);
    }

    private void ApplyRoleVisibility(string role)
    {
        switch (role)
        {
            case "ADMIN":
                menuAccounts.Visibility = Visibility.Visible;
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

    // ADMIN
    private void MenuAccountManagement_Click(object sender, RoutedEventArgs e)
        => new AccountManagementWindow().Show();
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
        => new TeacherScheduleWindow().Show();
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
        => new StudentScheduleWindow().Show();
    private void MenuAttendanceHistory_Click(object sender, RoutedEventArgs e)
        => new AttendanceHistoryWindow().Show();
    private void MenuMyGrades_Click(object sender, RoutedEventArgs e)
        => new StudentGradeWindow().Show();
    private void MenuMyInvoices_Click(object sender, RoutedEventArgs e)
        => new StudentInvoiceWindow().Show();

    private void MenuLogout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        this.Close();
    }
}