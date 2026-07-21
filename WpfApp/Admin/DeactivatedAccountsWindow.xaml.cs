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
    private List<User> _all = new();

    public DeactivatedAccountsWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        _all = _service.GetAll().Where(u => !u.IsActive).ToList();
        emptyState.Visibility = _all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BindPage();
    }

    private void BindPage() => dgUsers.ItemsSource = pager.Slice(_all);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void DgUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgUsers.SelectedItem is User u)
            new AccountViewWindow(u) { Owner = this }.ShowDialog();
    }

    private void BtnActivate_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u)
        {
            MessageBox.Show("Please select an account to activate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Activate account \"{u.Username}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            u.IsActive = true;
            _service.Update(u);
            LoadData();
        }
    }
}
