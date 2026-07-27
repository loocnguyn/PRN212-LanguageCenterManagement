using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  DeactivatedAccountsWindow — list soft-deleted accounts and
//  reactivate them.
//  CONTENTS:
//    1. Construction & load — inactive users into the grid
//    2. Row actions         — double-click to view; Activate
// ============================================================
public partial class DeactivatedAccountsWindow : Window
{
    private readonly IUserService _service = new UserService();
    private List<AccountRow> _all = new();

    public DeactivatedAccountsWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        var names = _service.GetDisplayNames();
        _all = _service.GetAll()
            .Where(u => !u.IsActive)
            .Select(u => new AccountRow(u, names.GetValueOrDefault(u.Id, "")))
            .ToList();
        emptyState.Visibility = _all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BindPage();
    }

    private void BindPage() => dgUsers.ItemsSource = pager.Slice(_all);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void DgUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgUsers.SelectedItem is AccountRow r)
            new AccountViewWindow(r.User) { Owner = this }.ShowDialog();
    }

    private void BtnActivate_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not AccountRow r)
        {
            MessageBox.Show("Please select an account to activate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Activate {r.Display}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            r.User.IsActive = true;
            _service.Update(r.User);
            LoadData();
        }
    }

    /// <summary>One grid row: the account plus the name of the person it belongs to.</summary>
    private sealed record AccountRow(User User, string FullName)
    {
        public string Email => User.Email;
        public string Role => User.Role;
        public DateTime CreatedAt => User.CreatedAt;
        public string Initials => string.IsNullOrWhiteSpace(FullName) ? Email : FullName;
        public string Display => string.IsNullOrWhiteSpace(FullName) ? Email : $"{FullName} ({Email})";
    }
}
