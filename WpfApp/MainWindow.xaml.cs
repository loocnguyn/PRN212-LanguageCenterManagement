using System.Windows;
using BusinessObjects;

namespace WpfApp;

public partial class MainWindow : Window
{
    private readonly User _currentUser;

    public MainWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        txtWelcome.Text = $"Xin chào, {currentUser.Username} ({currentUser.Role})";
    }

    private void MenuAccountManagement_Click(object sender, RoutedEventArgs e)
        => new AccountManagementWindow().Show();

    private void MenuStudents_Click(object sender, RoutedEventArgs e)
        => new StudentManagementWindow().Show();

    private void MenuTeachers_Click(object sender, RoutedEventArgs e)
        => new TeacherManagementWindow().Show();

    private void MenuCourses_Click(object sender, RoutedEventArgs e)
        => new CourseManagementWindow().Show();

    private void MenuClasses_Click(object sender, RoutedEventArgs e)
        => new ClassManagementWindow().Show();

    private void MenuLogout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        this.Close();
    }
}