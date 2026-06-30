using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class AccountManagementWindow : Window
{
    private readonly IUserService _service = new UserService();
    private List<User> _all = new();
    private User? _currentUser;

    public AccountManagementWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        LoadData();
    }

    private void LoadData()
    {
        _all = _service.GetAll();
        dgUsers.ItemsSource = _all;
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgUsers.ItemsSource = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(u => u.Username.ToLower().Contains(kw) || u.Role.ToLower().Contains(kw)).ToList();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Text = "";
        dgUsers.ItemsSource = _all;
    }

    private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AccountDetailWindow() { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u)
        {
            MessageBox.Show("Chọn một tài khoản để sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new AccountDetailWindow(u) { Owner = this };
        if (dlg.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgUsers.SelectedItem is not User u)
        {
            MessageBox.Show("Chọn một tài khoản để vô hiệu hóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_currentUser != null && u.Id == _currentUser.Id)
        {
            MessageBox.Show("Không thể vô hiệu hóa tài khoản đang đăng nhập.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Vô hiệu hóa tài khoản \"{u.Username}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _service.Delete(u.Id);
            LoadData();
        }
    }
}
