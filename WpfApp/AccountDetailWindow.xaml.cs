using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using DataAccessObjects;
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
            cmbRole.SelectedItem = cmbRole.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == user.Role);
            cmbRole.IsEnabled = false;
            txtUsername.Text = user.Username;
            txtUsername.IsEnabled = false;
            lblPasswordHint.Visibility = Visibility.Visible;
            LoadProfileFields(user);
        }
    }

    private void LoadProfileFields(User user)
    {
        ShowFieldsForRole(user.Role);

        switch (user.Role)
        {
            case "STUDENT":
                var s = StudentDAO.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (s != null)
                {
                    txtFullName.Text = s.FullName;
                    txtPhone.Text = s.Phone ?? "";
                    txtEmail.Text = s.Email ?? "";
                    if (s.DateOfBirth.HasValue)
                        dpDob.SelectedDate = s.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
                    SelectComboItem(cmbGender, s.Gender);
                }
                break;
            case "TEACHER":
                var t = TeacherDAO.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (t != null)
                {
                    txtFullName.Text = t.FullName;
                    txtPhone.Text = t.Phone ?? "";
                    txtEmail.Text = t.Email ?? "";
                    SelectComboItem(cmbGender, t.Gender);
                    SelectComboItem(txtSpec, t.Specialization);
                    SelectComboItem(cmbDegree, t.Degree);
                }
                break;
            case "STAFF":
                var st = StaffDAO.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (st != null)
                {
                    txtFullName.Text = st.FullName;
                    txtPhone.Text = st.Phone ?? "";
                    txtEmail.Text = st.Email ?? "";
                    SelectComboItem(txtDept, st.Department);
                }
                break;
            case "ADMIN":
                var a = AdminDAO.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (a != null)
                {
                    txtFullName.Text = a.FullName;
                    txtPhone.Text = a.Phone ?? "";
                    txtEmail.Text = a.Email ?? "";
                }
                break;
        }
    }

    private void SelectComboItem(ComboBox cmb, string? value)
    {
        if (value == null) return;
        cmb.SelectedItem = cmb.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == value);
    }

    private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editUser != null) return;
        var role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
        HideAllProfileFields();
        if (role != null) ShowFieldsForRole(role);
    }

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

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text.Trim().ToLower();
        var password = pwdPassword.Password;
        var role = _editUser?.Role ?? (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

        if (role == null)
        {
            MessageBox.Show("Role is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editUser == null)
        {
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Username is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (username.Length < 5 || username.Length > 20)
            {
                MessageBox.Show("Username must be 5–20 characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-z0-9]+$"))
            {
                MessageBox.Show("Username must contain only letters (a-z) and digits.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var fullName = txtFullName.Text.Trim();
        if (string.IsNullOrEmpty(fullName))
        {
            MessageBox.Show("Full name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(fullName, @"^[\p{L}\s]+$"))
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
        if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            MessageBox.Show("Phone number must be 10 digits and start with 0.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var email = txtEmail.Text.Trim();
        if (!string.IsNullOrEmpty(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            MessageBox.Show("Invalid email format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editUser == null)
        {
            if (password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_service.GetAll().Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Username already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (role == "ADMIN" && _service.GetAll().Any(u => u.Role == "ADMIN" && u.IsActive))
            {
                MessageBox.Show("Only one ADMIN account is allowed.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newUser = new User { Username = username, Role = role, IsActive = true };
            _service.Save(newUser, password);
            SaveProfile(newUser.Id, role, fullName, phone, email, isNew: true);
        }
        else
        {
            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _service.UpdatePassword(_editUser.Id, password);
            }

            SaveProfile(_editUser.Id, role, fullName, phone, email, isNew: false);
        }

        DialogResult = true;
    }

    private void SaveProfile(int userId, string role, string fullName, string phone, string email, bool isNew)
    {
        switch (role)
        {
            case "STUDENT":
                DateOnly? dob = dpDob.SelectedDate.HasValue
                    ? DateOnly.FromDateTime(dpDob.SelectedDate.Value) : null;
                var gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (isNew)
                {
                    StudentDAO.Save(new Student { UserId = userId, FullName = fullName, DateOfBirth = dob, Gender = gender, Phone = phone, Email = email, Status = "ACTIVE" });
                }
                else
                {
                    var s = StudentDAO.GetAll().FirstOrDefault(x => x.UserId == userId);
                    if (s != null) { s.FullName = fullName; s.DateOfBirth = dob; s.Gender = gender; s.Phone = phone; s.Email = email; StudentDAO.Update(s); }
                }
                break;
            case "TEACHER":
                var tGender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                var degree = (cmbDegree.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (isNew)
                {
                    TeacherDAO.Save(new Teacher { UserId = userId, FullName = fullName, Gender = tGender, Phone = phone, Email = email, Specialization = (txtSpec.SelectedItem as ComboBoxItem)?.Content.ToString(), Degree = degree, Status = "ACTIVE" });
                }
                else
                {
                    var t = TeacherDAO.GetAll().FirstOrDefault(x => x.UserId == userId);
                    if (t != null) { t.FullName = fullName; t.Gender = tGender; t.Phone = phone; t.Email = email; t.Specialization = (txtSpec.SelectedItem as ComboBoxItem)?.Content.ToString(); t.Degree = degree; TeacherDAO.Update(t); }
                }
                break;
            case "STAFF":
                if (isNew)
                {
                    StaffDAO.Save(new Staff { UserId = userId, FullName = fullName, Phone = phone, Email = email, Department = (txtDept.SelectedItem as ComboBoxItem)?.Content.ToString() });
                }
                else
                {
                    var st = StaffDAO.GetAll().FirstOrDefault(x => x.UserId == userId);
                    if (st != null) { st.FullName = fullName; st.Phone = phone; st.Email = email; st.Department = (txtDept.SelectedItem as ComboBoxItem)?.Content.ToString(); StaffDAO.Update(st); }
                }
                break;
            case "ADMIN":
                if (isNew)
                {
                    AdminDAO.Save(new Admin { UserId = userId, FullName = fullName, Phone = phone, Email = email });
                }
                else
                {
                    var a = AdminDAO.GetAll().FirstOrDefault(x => x.UserId == userId);
                    if (a != null) { a.FullName = fullName; a.Phone = phone; a.Email = email; AdminDAO.Update(a); }
                }
                break;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
