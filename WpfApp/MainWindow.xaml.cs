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
        => MessageBox.Show("Quản lý tài khoản — đang phát triển", "Thông báo");

    private void MenuStudents_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Quản lý học viên — đang phát triển", "Thông báo");

    private void MenuTeachers_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Quản lý giáo viên — đang phát triển", "Thông báo");

    private void MenuCourses_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Quản lý khóa học — đang phát triển", "Thông báo");

    private void MenuClasses_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Quản lý lớp học — đang phát triển", "Thông báo");

    private void MenuLogout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        this.Close();
    }
}