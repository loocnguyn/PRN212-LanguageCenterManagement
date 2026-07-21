using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  StaffManagementWindow — browse/manage staff + their departments.
//  CONTENTS:
//    1. Fields & construction   — services + department filter
//    2. Loading & filtering     — join staff to their user, search
//    3. Row actions             — view / add / edit / deactivate
//    4. StaffRow                — grid-facing view model
//  Add/Edit reuse AccountDetailWindow (role locked to STAFF).
// ============================================================
public partial class StaffManagementWindow : Window
{
    private readonly IStaffService _staffService = new StaffService();
    private readonly IUserService _userService = new UserService();
    private readonly IDepartmentService _departmentService = new DepartmentService();
    private List<StaffRow> _all = new();
    private List<StaffRow> _filtered = new();

    public StaffManagementWindow() { InitializeComponent(); LoadDeptFilter(); LoadData(); }

    private void LoadDeptFilter()
    {
        var items = new List<string> { "All" };
        items.AddRange(_departmentService.GetAll().Select(d => d.Name));
        cmbFilterDept.ItemsSource = items;
        cmbFilterDept.SelectedIndex = 0;
    }

    private void LoadData()
    {
        var users = _userService.GetAll().Where(u => u.IsActive && u.Role == "STAFF")
            .ToDictionary(u => u.Id);
        _all = _staffService.GetAll()
            .Where(s => users.ContainsKey(s.UserId))
            .Select(s => new StaffRow(s, users[s.UserId]))
            .OrderBy(r => r.FullName)
            .ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (dgStaff == null) return;
        var kw = txtSearch.Text.Trim().ToLower();
        var dept = cmbFilterDept.SelectedItem as string;

        _filtered = _all
            .Where(r => string.IsNullOrEmpty(kw)
                        || r.FullName.ToLower().Contains(kw)
                        || (r.Phone ?? "").Contains(kw)
                        || r.Username.ToLower().Contains(kw))
            .Where(r => dept is null or "All" || r.Department == dept)
            .ToList();

        dgStaff.ItemsSource = pager.Slice(_filtered);
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ApplyFilter();

    private void BtnSearch_Click(object sender, RoutedEventArgs e) { pager.Reset(); ApplyFilter(); }

    private void CmbFilterDept_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (dgStaff == null) return;
        pager.Reset();
        ApplyFilter();
    }

    private void DgStaff_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgStaff.SelectedItem is StaffRow r)
            new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnView_Click(object sender, RoutedEventArgs e)
    {
        if (dgStaff.SelectedItem is not StaffRow r) { Warn("view"); return; }
        new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AccountDetailWindow(presetRole: "STAFF") { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgStaff.SelectedItem is not StaffRow r) { Warn("edit"); return; }
        var dlg = new AccountDetailWindow(r.User) { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgStaff.SelectedItem is not StaffRow r) { Warn("deactivate"); return; }
        var confirm = MessageBox.Show($"Deactivate staff account \"{r.Username}\"?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes) { _userService.Delete(r.User.Id); LoadData(); }
    }

    private static void Warn(string action) =>
        MessageBox.Show($"Please select a staff member to {action}.", "Info",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private record StaffRow(Staff Staff, User User)
    {
        public string FullName => Staff.FullName;
        public string Username => User.Username;
        public string? Department => Staff.Department;
        public string? Phone => Staff.Phone;
        public string? Email => Staff.Email;
    }
}
