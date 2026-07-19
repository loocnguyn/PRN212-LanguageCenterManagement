using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  TeacherManagementWindow — browse/manage teacher profiles.
//  CONTENTS:
//    1. Fields & construction  — services + initial load
//    2. Loading & filtering    — join teachers to their user, search
//    3. Row actions            — view / add / edit / deactivate
//    4. TeacherRow             — grid-facing view model
//  Add/Edit reuse AccountDetailWindow (role locked to TEACHER);
//  View reuses AccountViewWindow; Deactivate soft-deletes the user.
// ============================================================
public partial class TeacherManagementWindow : Window
{
    // ---- 1. Fields & construction ------------------------------
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IUserService _userService = new UserService();
    private List<TeacherRow> _all = new();

    public TeacherManagementWindow() { InitializeComponent(); LoadData(); }

    // ---- 2. Loading & filtering --------------------------------
    private void LoadData()
    {
        var users = _userService.GetAll()
            .Where(u => u.IsActive && u.Role == "TEACHER")
            .ToDictionary(u => u.Id);
        _all = _teacherService.GetAll()
            .Where(t => users.ContainsKey(t.UserId))
            .Select(t => new TeacherRow(t, users[t.UserId]))
            .OrderBy(r => r.FullName)
            .ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgTeachers.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(r => r.FullName.ToLower().Contains(kw)
                              || (r.Phone ?? "").Contains(kw)
                              || r.Username.ToLower().Contains(kw)).ToList();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; ApplyFilter(); }

    // ---- 3. Row actions ----------------------------------------
    private void DgTeachers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgTeachers.SelectedItem is TeacherRow r)
            new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnView_Click(object sender, RoutedEventArgs e)
    {
        if (dgTeachers.SelectedItem is not TeacherRow r) { Warn("view"); return; }
        new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AccountDetailWindow(presetRole: "TEACHER") { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgTeachers.SelectedItem is not TeacherRow r) { Warn("edit"); return; }
        var dlg = new AccountDetailWindow(r.User) { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgTeachers.SelectedItem is not TeacherRow r) { Warn("deactivate"); return; }
        var confirm = MessageBox.Show($"Deactivate teacher account \"{r.Username}\"?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes) { _userService.Delete(r.User.Id); LoadData(); }
    }

    private static void Warn(string action) =>
        MessageBox.Show($"Please select a teacher to {action}.", "Info",
            MessageBoxButton.OK, MessageBoxImage.Information);

    // ---- 4. TeacherRow (grid-facing view model) ----------------
    private record TeacherRow(Teacher Teacher, User User)
    {
        public string FullName => Teacher.FullName;
        public string Username => User.Username;
        public string? Specialization => Teacher.Specialization;
        public string? Degree => Teacher.Degree;
        public string? Phone => Teacher.Phone;
        public string? Email => Teacher.Email;
    }
}
