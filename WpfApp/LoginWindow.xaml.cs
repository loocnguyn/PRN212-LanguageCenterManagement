using System.Windows;
using Services;

namespace WpfApp;

public partial class LoginWindow : Window
{
    private readonly IUserService _userService = new UserService();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = pwdPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var user = _userService.Login(username, password);
        if (user == null)
        {
            MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.", "Đăng nhập thất bại",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var main = new MainWindow(user);
        main.Show();
        this.Close();
    }
}
