using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class AccountManagementWindow : Window
{
    private readonly IUserService _service = new UserService();
    private List<User> _all = new();

    public AccountManagementWindow() { InitializeComponent(); LoadData(); }
    private void LoadData() { _all = _service.GetAll(); dgUsers.ItemsSource = _all; }
    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgUsers.ItemsSource = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(u => u.Username.ToLower().Contains(kw) || u.Role.ToLower().Contains(kw)).ToList();
    }
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; dgUsers.ItemsSource = _all; }
    private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void BtnAdd_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Add account — TODO");
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u) { MessageBox.Show("Please select a user."); return; }
        MessageBox.Show($"Edit: {u.Username} — TODO");
    }
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u) { MessageBox.Show("Please select a user."); return; }
        var confirm = MessageBox.Show($"Delete {u.Username}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(u.Id); LoadData(); }
    }
}
