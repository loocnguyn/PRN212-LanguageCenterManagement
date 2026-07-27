using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ChangePasswordWindow — forced first-login password change.
//
//  Shown by LoginWindow when the account still carries the password somebody
//  else typed for it (Admin created it, or it came in through the student
//  import). There is no way past it other than choosing a password or signing
//  out: DialogResult stays false unless the save succeeds.
//
//  UserService.UpdatePassword clears MustChangePassword, so the next login
//  goes straight through.
// ============================================================
public partial class ChangePasswordWindow : Window
{
    private readonly IUserService _userService = new UserService();
    private readonly User _user;

    public ChangePasswordWindow(User user)
    {
        InitializeComponent();
        _user = user;
        tbSubtitle.Text = $"{user.Email} is still using the password it was created with. "
                        + "Pick a new one to continue.";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var newPassword = pwdNew.Password;
        var confirm = pwdConfirm.Password;

        if (!ValidationHelper.IsValidPassword(newPassword))
        {
            ShowError($"Password must be at least {ValidationHelper.MinPasswordLength} characters.");
            return;
        }

        if (newPassword != confirm)
        {
            ShowError("The two passwords do not match.");
            return;
        }

        // Re-using the starting password would leave the account exactly as exposed
        // as it was — the whole reason we are on this screen.
        if (_userService.Login(_user.Email, newPassword) != null)
        {
            ShowError("Please choose a password different from the one you were given.");
            return;
        }

        try
        {
            _userService.UpdatePassword(_user.Id, newPassword);
            _user.MustChangePassword = false;

            MessageBox.Show("Password updated. Welcome!", "Done",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowError($"Could not update the password: {ex.Message}");
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ShowError(string message)
    {
        tbError.Text = message;
        tbError.Visibility = Visibility.Visible;
    }
}
