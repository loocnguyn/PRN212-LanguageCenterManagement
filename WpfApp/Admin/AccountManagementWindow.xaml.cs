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
    private List<AccountRow> _all = new();
    private List<AccountRow> _filtered = new();

    public AccountManagementWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        LoadData();
    }

    private void LoadData()
    {
        // The person's name comes from their profile table; fetched once for the whole
        // list rather than per row.
        var names = _service.GetDisplayNames();
        _all = _service.GetAll()
            .Where(u => u.IsActive)
            .Select(u => new AccountRow(u, names.GetValueOrDefault(u.Id, "")))
            .ToList();
        pager.Reset();
        UpdateStats();
        ApplyFilter();
    }

    private void UpdateStats()
    {
        statTotal.Text = _all.Count.ToString();
        statAdmin.Text = _all.Count(r => r.Role == "ADMIN").ToString();
        statStaff.Text = _all.Count(r => r.Role == "STAFF").ToString();
        statTeacher.Text = _all.Count(r => r.Role == "TEACHER").ToString();
        statStudent.Text = _all.Count(r => r.Role == "STUDENT").ToString();
    }

    private void ApplyFilter()
    {
        if (dgUsers == null) return;

        var kw = txtSearch.Text.Trim().ToLower();
        var role = (cmbFilterRole.SelectedItem as ComboBoxItem)?.Content.ToString();

        // Searching by name as well as email: an admin looking for a person thinks of
        // the name first.
        _filtered = _all.AsEnumerable()
            .Where(r => string.IsNullOrEmpty(kw)
                        || r.Email.ToLower().Contains(kw)
                        || r.FullName.ToLower().Contains(kw))
            .Where(r => role == "All" || string.IsNullOrEmpty(role) || r.Role == role)
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

    private void DgUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgUsers.SelectedItem is AccountRow) ShowSelectedAccount();
    }

    private void BtnView_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not AccountRow)
        {
            MessageBox.Show("Please select an account to view.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ShowSelectedAccount();
    }

    private void ShowSelectedAccount()
    {
        if (dgUsers.SelectedItem is not AccountRow r) return;
        new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AccountDetailWindow() { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not AccountRow r)
        {
            MessageBox.Show("Please select an account to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new AccountDetailWindow(r.User) { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not AccountRow r)
        {
            MessageBox.Show("Please select an account to deactivate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var u = r.User;
        if (u.Id == _currentUser.Id)
        {
            MessageBox.Show("Cannot deactivate the currently logged-in account.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Deactivate {r.Display}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _service.Delete(u.Id);
            LoadData();
        }
    }

    /// <summary>
    /// One grid row: the account plus the name of the person it belongs to. The name
    /// lives in a profile table, so it cannot come off the User itself.
    /// </summary>
    private sealed record AccountRow(User User, string FullName)
    {
        public string Email => User.Email;
        public string Role => User.Role;
        public DateTime CreatedAt => User.CreatedAt;

        /// <summary>What the avatar takes its initials from — the name, or the email
        /// when an account has no profile row yet.</summary>
        public string Initials => string.IsNullOrWhiteSpace(FullName) ? Email : FullName;

        public string Display => string.IsNullOrWhiteSpace(FullName) ? Email : $"{FullName} ({Email})";
    }
}
