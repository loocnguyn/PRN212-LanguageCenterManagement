using System.Windows;
using Services;

namespace WpfApp;

// LoginWindow — email/password sign-in; on success opens MainWindow for the user's role.
//
// Accounts whose password was set by somebody else (Admin-created, CSV-imported)
// come back with MustChangePassword = true. They are made to pick their own
// password here, before MainWindow opens — see ChangePasswordWindow.
public partial class LoginWindow : Window
{
    private readonly IUserService _userService = new UserService();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string email = txtEmail.Text.Trim().ToLower();
        string password = pwdPassword.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Please enter your email and password.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var user = _userService.Login(email, password);
            if (user == null)
            {
                // Deliberately one message for "no such email", "wrong password" and
                // "deactivated": saying which would let anyone test whether an address
                // has an account here.
                MessageBox.Show("Invalid email or password.", "Login Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (user.MustChangePassword)
            {
                var dialog = new ChangePasswordWindow(user) { Owner = this };
                if (dialog.ShowDialog() != true) return;   // cancelled — stay on the login screen
            }

            var main = new MainWindow(user);
            main.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Login failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
