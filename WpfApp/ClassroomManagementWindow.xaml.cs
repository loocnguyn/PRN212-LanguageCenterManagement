using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassroomManagementWindow : Window
{
    private readonly IClassroomService _service = new ClassroomService();
    private List<Classroom> _all = new();

    public ClassroomManagementWindow() { InitializeComponent(); LoadData(); }
    private void LoadData() { _all = _service.GetAll(); dgClassrooms.ItemsSource = _all; }
    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgClassrooms.ItemsSource = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(r => r.Name.ToLower().Contains(kw)).ToList();
    }
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; dgClassrooms.ItemsSource = _all; }
    private void DgClassrooms_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void BtnAdd_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Add classroom — TODO");
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgClassrooms.SelectedItem is not Classroom r) { MessageBox.Show("Please select a classroom."); return; }
        MessageBox.Show($"Edit: {r.Name} — TODO");
    }
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgClassrooms.SelectedItem is not Classroom r) { MessageBox.Show("Please select a classroom."); return; }
        var confirm = MessageBox.Show($"Delete {r.Name}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(r.ClassroomId); LoadData(); }
    }
}
