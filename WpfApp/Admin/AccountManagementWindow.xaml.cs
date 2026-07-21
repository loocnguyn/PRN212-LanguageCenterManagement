using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  AccountManagementWindow — list & manage all active accounts.
//  CONTENTS:
//    1. Fields & construction  — current user
//    2. Load / stats / filter  — load actives, role counts, search+role filter
//    3. Paging                 — PagerBar slices the filtered list
//    4. Row actions            — view / add / edit / deactivate
//  View->AccountViewWindow, Add/Edit->AccountDetailWindow.
// ============================================================
public partial class AccountManagementWindow : Window
{
    private readonly IUserService _service = new UserService();
    private readonly User _currentUser;
    private List<User> _all = new();
    private List<User> _filtered = new();

    public AccountManagementWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        LoadData();
    }

    private void LoadData()
    {
        _all = _service.GetAll().Where(u => u.IsActive).ToList();
        pager.Reset();
        UpdateStats();
        ApplyFilter();
    }

    private void UpdateStats()
    {
        statTotal.Text = _all.Count.ToString();
        statAdmin.Text = _all.Count(u => u.Role == "ADMIN").ToString();
        statStaff.Text = _all.Count(u => u.Role == "STAFF").ToString();
        statTeacher.Text = _all.Count(u => u.Role == "TEACHER").ToString();
        statStudent.Text = _all.Count(u => u.Role == "STUDENT").ToString();
    }

    private void ApplyFilter()
    {
        if (dgUsers == null) return;

        var kw = txtSearch.Text.Trim().ToLower();
        var role = (cmbFilterRole.SelectedItem as ComboBoxItem)?.Content.ToString();

        _filtered = _all.AsEnumerable()
            .Where(u => string.IsNullOrEmpty(kw) || u.Username.ToLower().Contains(kw))
            .Where(u => role == "All" || string.IsNullOrEmpty(role) || u.Role == role)
            .ToList();

        dgUsers.ItemsSource = pager.Slice(_filtered);
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ApplyFilter();

    private void BtnSearch_Click(object sender, RoutedEventArgs e) { pager.Reset(); ApplyFilter(); }

    private void CmbFilterRole_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (dgUsers == null) return;
        pager.Reset();
        ApplyFilter();
    }

    private void BtnShowDeactivated_Click(object sender, RoutedEventArgs e)
    {
        new DeactivatedAccountsWindow() { Owner = this }.ShowDialog();
        LoadData();
    }

    private void DgUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgUsers.SelectedItem is User) ShowSelectedAccount();
    }

    private void BtnView_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User)
        {
            MessageBox.Show("Please select an account to view.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ShowSelectedAccount();
    }

    private void ShowSelectedAccount()
    {
        if (dgUsers.SelectedItem is not User u) return;
        new AccountViewWindow(u) { Owner = this }.ShowDialog();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AccountDetailWindow() { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u)
        {
            MessageBox.Show("Please select an account to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new AccountDetailWindow(u) { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u)
        {
            MessageBox.Show("Please select an account to deactivate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (u.Id == _currentUser.Id)
        {
            MessageBox.Show("Cannot deactivate the currently logged-in account.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Deactivate account \"{u.Username}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _service.Delete(u.Id);
            LoadData();
        }
    }
}
