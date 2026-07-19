using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  StudentManagementWindow — browse/manage student profiles.
//  CONTENTS:
//    1. Fields & construction  — services + initial load
//    2. Loading & filtering    — join students to their user, search
//    3. Row actions            — view / add / edit / deactivate
//    4. StudentRow             — grid-facing view model
//  Add/Edit reuse AccountDetailWindow (role locked to STUDENT);
//  View reuses AccountViewWindow; Deactivate soft-deletes the user.
// ============================================================
public partial class StudentManagementWindow : Window
{
    // ---- 1. Fields & construction ------------------------------
    private readonly IStudentService _studentService = new StudentService();
    private readonly IUserService _userService = new UserService();
    private List<StudentRow> _all = new();

    public StudentManagementWindow() { InitializeComponent(); LoadData(); }

    // ---- 2. Loading & filtering --------------------------------
    private void LoadData()
    {
        var users = _userService.GetAll()
            .Where(u => u.IsActive && u.Role == "STUDENT")
            .ToDictionary(u => u.Id);
        _all = _studentService.GetAll()
            .Where(s => users.ContainsKey(s.UserId))
            .Select(s => new StudentRow(s, users[s.UserId]))
            .OrderBy(r => r.FullName)
            .ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgStudents.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(r => r.FullName.ToLower().Contains(kw)
                              || (r.Phone ?? "").Contains(kw)
                              || r.Username.ToLower().Contains(kw)).ToList();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; ApplyFilter(); }

    // ---- 3. Row actions ----------------------------------------
    private void DgStudents_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgStudents.SelectedItem is StudentRow r)
            new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnView_Click(object sender, RoutedEventArgs e)
    {
        if (dgStudents.SelectedItem is not StudentRow r) { Warn("view"); return; }
        new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AccountDetailWindow(presetRole: "STUDENT") { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgStudents.SelectedItem is not StudentRow r) { Warn("edit"); return; }
        var dlg = new AccountDetailWindow(r.User) { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgStudents.SelectedItem is not StudentRow r) { Warn("deactivate"); return; }
        var confirm = MessageBox.Show($"Deactivate student account \"{r.Username}\"?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes) { _userService.Delete(r.User.Id); LoadData(); }
    }

    private static void Warn(string action) =>
        MessageBox.Show($"Please select a student to {action}.", "Info",
            MessageBoxButton.OK, MessageBoxImage.Information);

    // ---- 4. StudentRow (grid-facing view model) ----------------
    private record StudentRow(Student Student, User User)
    {
        public string FullName => Student.FullName;
        public string Username => User.Username;
        public string? Gender => Student.Gender;
        public DateOnly? DateOfBirth => Student.DateOfBirth;
        public string? Phone => Student.Phone;
        public string? Email => Student.Email;
    }
}
