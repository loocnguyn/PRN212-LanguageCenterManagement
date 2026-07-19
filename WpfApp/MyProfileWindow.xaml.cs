using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class MyProfileWindow : Window
{
    private readonly User _currentUser;
    private readonly IUserService _userService = new UserService();
    private readonly IStudentService _studentService = new StudentService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IStaffService _staffService = new StaffService();
    private readonly IAdminService _adminService = new AdminService();

    private Student? _student;
    private Teacher? _teacher;
    private Staff? _staff;
    private Admin? _admin;

    public MyProfileWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadProfile();
    }

    private void LoadProfile()
    {
        try
        {
            txtUsername.Text = _currentUser.Username;
            tbRoleBadge.Text = _currentUser.Role;
            header.Background = RoleBrush(_currentUser.Role);
            txtAvatar.Foreground = RoleBrush(_currentUser.Role);

            switch (_currentUser.Role)
            {
                case "STUDENT":
                    _student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
                    if (_student == null) { ShowNotFound(); return; }
                    txtFullName.Text = _student.FullName;
                    txtPhone.Text = _student.Phone ?? "";
                    txtEmail.Text = _student.Email ?? "";
                    ShowField(lblAddress, txtAddress);
                    txtAddress.Text = _student.Address ?? "";
                    ShowField(lblGender, cmbGender);
                    SelectComboItem(cmbGender, _student.Gender);
                    break;

                case "TEACHER":
                    _teacher = _teacherService.GetByUserId(_currentUser.Id);
                    if (_teacher == null) { ShowNotFound(); return; }
                    txtFullName.Text = _teacher.FullName;
                    txtPhone.Text = _teacher.Phone ?? "";
                    txtEmail.Text = _teacher.Email ?? "";
                    ShowField(lblGender, cmbGender);
                    SelectComboItem(cmbGender, _teacher.Gender);
                    ShowField(lblSpec, cmbSpec);
                    SelectComboItem(cmbSpec, _teacher.Specialization);
                    cmbSpec.IsEnabled = false;
                    break;

                case "STAFF":
                    _staff = _staffService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
                    if (_staff == null) { ShowNotFound(); return; }
                    txtFullName.Text = _staff.FullName;
                    txtPhone.Text = _staff.Phone ?? "";
                    txtEmail.Text = _staff.Email ?? "";
                    ShowField(lblGender, cmbGender);
                    SelectComboItem(cmbGender, _staff.Gender);
                    ShowField(lblDept, cmbDept);
                    SelectComboItem(cmbDept, _staff.Department);
                    cmbDept.IsEnabled = false;
                    break;

                case "ADMIN":
                    _admin = _adminService.GetAll().FirstOrDefault(a => a.UserId == _currentUser.Id);
                    if (_admin == null) { ShowNotFound(); return; }
                    txtFullName.Text = _admin.FullName;
                    txtPhone.Text = _admin.Phone ?? "";
                    txtEmail.Text = _admin.Email ?? "";
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowNotFound()
    {
        txtFullName.Text = "No profile linked to this account.";
        txtPhone.IsEnabled = false;
        txtEmail.IsEnabled = false;
    }

    private static void ShowField(UIElement label, UIElement input)
    {
        label.Visibility = Visibility.Visible;
        input.Visibility = Visibility.Visible;
    }

    private static void SelectComboItem(ComboBox cmb, string? value)
    {
        if (value == null) return;
        cmb.SelectedItem = cmb.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == value);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var phone = txtPhone.Text.Trim();
        if (!ValidationHelper.IsValidPhone(phone))
        {
            MessageBox.Show("Phone number must be 10 digits and start with 0.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var email = txtEmail.Text.Trim();
        if (!ValidationHelper.IsValidEmail(email))
        {
            MessageBox.Show("Invalid email format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var oldPassword = pwdOld.Password;
        var newPassword = pwdNew.Password;
        if (!string.IsNullOrEmpty(oldPassword) || !string.IsNullOrEmpty(newPassword))
        {
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Please fill in both old and new password to change it.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!ValidationHelper.IsValidPassword(newPassword))
            {
                MessageBox.Show($"New password must be at least {ValidationHelper.MinPasswordLength} characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_userService.Login(_currentUser.Username, oldPassword) == null)
            {
                MessageBox.Show("Old password is incorrect.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            switch (_currentUser.Role)
            {
                case "STUDENT" when _student != null:
                    _student.Phone = phone;
                    _student.Email = email;
                    _student.Address = txtAddress.Text.Trim();
                    _student.Gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                    _studentService.Update(_student);
                    break;

                case "TEACHER" when _teacher != null:
                    _teacher.Phone = phone;
                    _teacher.Email = email;
                    _teacher.Gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                    // Specialization is fixed at account creation — only Admin can change it (Account Management).
                    _teacherService.Update(_teacher);
                    break;

                case "STAFF" when _staff != null:
                    _staff.Phone = phone;
                    _staff.Email = email;
                    _staff.Gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                    // Department is fixed at account creation — only Admin can change it (Account Management).
                    _staffService.Update(_staff);
                    break;

                case "ADMIN" when _admin != null:
                    _admin.Phone = phone;
                    _admin.Email = email;
                    _adminService.Update(_admin);
                    break;
            }

            if (!string.IsNullOrEmpty(newPassword))
                _userService.UpdatePassword(_currentUser.Id, newPassword);

            MessageBox.Show("Profile updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            pwdOld.Password = "";
            pwdNew.Password = "";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private System.Windows.Media.Brush RoleBrush(string role) => role switch
    {
        "ADMIN" => (System.Windows.Media.Brush)FindResource("RoleAdminBrush"),
        "TEACHER" => (System.Windows.Media.Brush)FindResource("RoleTeacherBrush"),
        "STUDENT" => (System.Windows.Media.Brush)FindResource("RoleStudentBrush"),
        _ => (System.Windows.Media.Brush)FindResource("RoleStaffBrush"),
    };

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
}
