using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  AccountDetailWindow — the Add/Edit dialog for one user account.
//  One dialog serves every role; the visible fields change with the
//  selected role. On Add the role is chosen (or preset+locked); on
//  Edit the role is read-only.
//
//  The Email field is the account's LOGIN. It is written to Users.email (unique,
//  the credential) and to the role's profile row (contact detail) at the same
//  time, so the two can never disagree.
//  CONTENTS:
//    1. Fields & construction   — edit vs add vs preset-role modes
//    2. LoadProfileFields       — fill fields from the role's profile table
//    3. Role field visibility   — ShowFieldsForRole / HideAll / Show
//    4. Save & validation       — validate, then create or update
//    5. SaveProfile             — write the role-specific profile row
//    6. Helpers                 — SelectComboItem, Cancel
// ============================================================
public partial class AccountDetailWindow : Window
{
    private readonly IUserService _service = new UserService();
    private readonly IDepartmentService _departmentService = new DepartmentService();
    private readonly IStudentService _studentService = new StudentService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IStaffService _staffService = new StaffService();
    private readonly IAdminService _adminService = new AdminService();
    private readonly User? _editUser;

    public AccountDetailWindow(User? user = null, string? presetRole = null)
    {
        InitializeComponent();
        _editUser = user;

        // Department options come from the managed Departments table.
        txtDept.ItemsSource = _departmentService.GetAll().Select(d => d.Name).ToList();

        if (user != null)
        {
            Title = "Edit Account";
            cmbRole.SelectedItem = cmbRole.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == user.Role);
            cmbRole.IsEnabled = false;
            lblPasswordHint.Visibility = Visibility.Visible;
            LoadProfileFields(user);
        }
        else if (presetRole != null)
        {
            // Opened from a role-specific screen (e.g. Staff/Student Management):
            // lock the role so this dialog only creates that kind of account.
            cmbRole.SelectedItem = cmbRole.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == presetRole);
            cmbRole.IsEnabled = false;
            ShowFieldsForRole(presetRole);
        }
    }

    // ---- 2. Load profile fields (edit mode) --------------------
    private void LoadProfileFields(User user)
    {
        ShowFieldsForRole(user.Role);

        // Users.email is the credential and therefore the source of truth; the
        // profile tables just keep a copy of it.
        txtEmail.Text = user.Email;

        switch (user.Role)
        {
            case "STUDENT":
                var s = _studentService.GetByUserId(user.Id);
                if (s != null)
                {
                    txtFullName.Text = s.FullName;
                    txtPhone.Text = s.Phone ?? "";
                    if (s.DateOfBirth.HasValue)
                        dpDob.SelectedDate = s.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
                    ComboBoxHelper.Select(cmbGender, s.Gender);
                }
                break;

            case "TEACHER":
                var t = _teacherService.GetByUserId(user.Id);
                if (t != null)
                {
                    txtFullName.Text = t.FullName;
                    txtPhone.Text = t.Phone ?? "";
                    ComboBoxHelper.Select(cmbGender, t.Gender);
                    ComboBoxHelper.Select(txtSpec, t.Specialization);
                    ComboBoxHelper.Select(cmbDegree, t.Degree);
                }
                break;

            case "STAFF":
                var st = _staffService.GetByUserId(user.Id);
                if (st != null)
                {
                    txtFullName.Text = st.FullName;
                    txtPhone.Text = st.Phone ?? "";
                    txtDept.SelectedItem = st.Department;
                }
                break;

            case "ADMIN":
                var a = _adminService.GetByUserId(user.Id);
                if (a != null)
                {
                    txtFullName.Text = a.FullName;
                    txtPhone.Text = a.Phone ?? "";
                }
                break;
        }
    }

    private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editUser != null) return;
        var role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
        HideAllProfileFields();
        if (role != null) ShowFieldsForRole(role);
    }

    // ---- 3. Role-driven field visibility -----------------------
    private void ShowFieldsForRole(string role)
    {
        HideAllProfileFields();
        switch (role)
        {
            case "STUDENT":
                Show(lblFullName, txtFullName, lblPhone, txtPhone, lblEmail, txtEmail,
                     lblDob, dpDob, lblGender, cmbGender);
                break;
            case "TEACHER":
                Show(lblFullName, txtFullName, lblPhone, txtPhone, lblEmail, txtEmail,
                     lblGender, cmbGender, lblSpec, txtSpec, lblDegree, cmbDegree);
                break;
            case "STAFF":
                Show(lblFullName, txtFullName, lblPhone, txtPhone, lblEmail, txtEmail,
                     lblDept, txtDept);
                break;
            case "ADMIN":
                Show(lblFullName, txtFullName, lblPhone, txtPhone, lblEmail, txtEmail);
                break;
        }
    }

    private void HideAllProfileFields()
    {
        var controls = new UIElement[] {
            lblFullName, txtFullName, lblPhone, txtPhone, lblEmail, txtEmail,
            lblDob, dpDob, lblGender, cmbGender,
            lblSpec, txtSpec, lblDegree, cmbDegree,
            lblDept, txtDept
        };
        foreach (var c in controls) c.Visibility = Visibility.Collapsed;
    }

    private void Show(params UIElement[] controls)
    {
        foreach (var c in controls) c.Visibility = Visibility.Visible;
    }

    // ---- 4. Save & validation ----------------------------------
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var password = pwdPassword.Password;
        var role = _editUser?.Role ?? (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

        if (role == null)
        {
            MessageBox.Show("Role is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fullName = txtFullName.Text.Trim();
        if (string.IsNullOrEmpty(fullName))
        {
            MessageBox.Show("Full name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidationHelper.IsValidFullName(fullName))
        {
            MessageBox.Show("Full name must contain only letters and spaces.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (role == "STAFF" && txtDept.SelectedItem == null)
        {
            MessageBox.Show("Department is required for Staff.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var phone = txtPhone.Text.Trim();
        if (!ValidationHelper.IsValidPhone(phone))
        {
            MessageBox.Show("Phone number must be 10 digits and start with 0.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // The email is the login, so it is required for every role and must be free.
        var email = txtEmail.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(email))
        {
            MessageBox.Show("Email is required — it is what this person signs in with.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidationHelper.IsValidEmail(email))
        {
            MessageBox.Show("Invalid email format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_service.IsEmailTaken(email, _editUser?.Id))
        {
            MessageBox.Show($"{email} is already used by another account.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editUser == null)
        {
            if (!ValidationHelper.IsValidPassword(password))
            {
                MessageBox.Show($"Password must be at least {ValidationHelper.MinPasswordLength} characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (role == "ADMIN" && _service.GetAll().Any(u => u.Role == "ADMIN" && u.IsActive))
            {
                MessageBox.Show("Only one ADMIN account is allowed.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save(...) defaults MustChangePassword to true: the admin picked this
            // password, so the owner has to replace it at their first login.
            var newUser = new User { Email = email, Role = role, IsActive = true };
            _service.Save(newUser, password);
            SaveProfile(newUser.Id, role, fullName, phone, email, isNew: true);
        }
        else
        {
            if (!string.IsNullOrEmpty(password))
            {
                if (!ValidationHelper.IsValidPassword(password))
                {
                    MessageBox.Show($"Password must be at least {ValidationHelper.MinPasswordLength} characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _service.UpdatePassword(_editUser.Id, password);
            }

            // Changing the email here changes how this person signs in.
            if (!_editUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                _editUser.Email = email;
                _service.Update(_editUser);
            }

            SaveProfile(_editUser.Id, role, fullName, phone, email, isNew: false);
        }

        DialogResult = true;
    }

    // ---- 5. Save the role-specific profile row -----------------
    //
    //  Each role writes to a different table but the shape is identical: take the
    //  existing row (or a new one), copy the form onto it, then Save or Update.
    //  Writing that out twice per role is what made this method four times longer
    //  than it needed to be, and is how the create and edit paths drift apart.
    private void SaveProfile(int userId, string role, string fullName, string phone, string email, bool isNew)
    {
        var gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();

        switch (role)
        {
            case "STUDENT":
                var student = isNew
                    ? new Student { UserId = userId, Status = "ACTIVE" }
                    : _studentService.GetByUserId(userId);
                if (student == null) return;

                student.FullName = fullName;
                student.Phone = phone;
                student.Email = email;
                student.Gender = gender;
                student.DateOfBirth = dpDob.SelectedDate.HasValue
                    ? DateOnly.FromDateTime(dpDob.SelectedDate.Value)
                    : null;

                if (isNew) _studentService.Save(student); else _studentService.Update(student);
                break;

            case "TEACHER":
                var teacher = isNew
                    ? new Teacher { UserId = userId, Status = "ACTIVE" }
                    : _teacherService.GetByUserId(userId);
                if (teacher == null) return;

                teacher.FullName = fullName;
                teacher.Phone = phone;
                teacher.Email = email;
                teacher.Gender = gender;
                teacher.Specialization = (txtSpec.SelectedItem as ComboBoxItem)?.Content.ToString();
                teacher.Degree = (cmbDegree.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (isNew) _teacherService.Save(teacher); else _teacherService.Update(teacher);
                break;

            case "STAFF":
                var staff = isNew
                    ? new Staff { UserId = userId }
                    : _staffService.GetByUserId(userId);
                if (staff == null) return;

                staff.FullName = fullName;
                staff.Phone = phone;
                staff.Email = email;
                staff.Department = txtDept.SelectedItem as string;

                if (isNew) _staffService.Save(staff); else _staffService.Update(staff);
                break;

            case "ADMIN":
                var admin = isNew
                    ? new Admin { UserId = userId }
                    : _adminService.GetByUserId(userId);
                if (admin == null) return;

                admin.FullName = fullName;
                admin.Phone = phone;
                admin.Email = email;

                if (isNew) _adminService.Save(admin); else _adminService.Update(admin);
                break;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
