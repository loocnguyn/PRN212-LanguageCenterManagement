using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class AccountDetailWindow : Window
{
    private readonly IUserService _service = new UserService();
    private readonly User? _editUser;

    public AccountDetailWindow(User? user = null)
    {
        InitializeComponent();
        _editUser = user;

        if (user != null)
        {
            Title = "Edit Account";
            txtUsername.Text = user.Username;
            txtUsername.IsEnabled = false;
            cmbRole.SelectedItem = cmbRole.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == user.Role);
            chkActive.IsChecked = user.IsActive;
            lblPasswordHint.Visibility = Visibility.Visible;
        }
        else
        {
            chkActive.IsChecked = true;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var password = pwdPassword.Password;
        var role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
        var isActive = chkActive.IsChecked == true;

        if (string.IsNullOrEmpty(username) || role == null)
        {
            MessageBox.Show("Username và Role không được để trống.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (username.Length < 3 || username.Length > 50)
        {
            MessageBox.Show("Username phải từ 3 đến 50 ký tự.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editUser == null)
        {
            if (password.Length < 6)
            {
                MessageBox.Show("Password phải ít nhất 6 ký tự.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_service.GetByUsername(username) != null)
            {
                MessageBox.Show("Username đã tồn tại.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newUser = new User { Username = username, Role = role, IsActive = isActive };
            ((UserService)_service).Save(newUser, password);
        }
        else
        {
            _editUser.Role = role;
            _editUser.IsActive = isActive;
            _service.Update(_editUser);

            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 6)
                {
                    MessageBox.Show("Password phải ít nhất 6 ký tự.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ((UserService)_service).UpdatePassword(_editUser.Id, password);
            }
        }

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
