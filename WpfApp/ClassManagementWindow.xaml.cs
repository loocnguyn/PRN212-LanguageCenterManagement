using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassManagementWindow : Window
{
    private readonly IClassService _service = new ClassService();
    private List<Class> _all = new();

    public ClassManagementWindow() { InitializeComponent(); LoadData(); }
    private void LoadData() { _all = _service.GetAll(); dgClasses.ItemsSource = _all; }
    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgClasses.ItemsSource = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(c => c.Name.ToLower().Contains(kw) || c.Status.ToLower().Contains(kw)).ToList();
    }
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; dgClasses.ItemsSource = _all; }
    private void DgClasses_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ClassDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _service.Save(dialog.Result);
            LoadData();
        }
    }
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgClasses.SelectedItem is not Class c) { MessageBox.Show("Please select a class."); return; }
        var dialog = new ClassDialog(c);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _service.Update(dialog.Result);
            LoadData();
        }
    }
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgClasses.SelectedItem is not Class c) { MessageBox.Show("Please select a class."); return; }
        var confirm = MessageBox.Show($"Delete {c.Name}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(c.ClassId); LoadData(); }
    }
}
