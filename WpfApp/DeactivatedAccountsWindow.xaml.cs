using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class DeactivatedAccountsWindow : Window
{
    private readonly IUserService _service = new UserService();

    public DeactivatedAccountsWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        dgUsers.ItemsSource = _service.GetAll().Where(u => !u.IsActive).ToList();
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
