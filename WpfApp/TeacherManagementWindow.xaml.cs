using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class TeacherManagementWindow : Window
{
    private readonly ITeacherService _service = new TeacherService();
    private List<Teacher> _all = new();

    public TeacherManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        _all = _service.GetAll();
        dgTeachers.ItemsSource = _all;
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgTeachers.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(t => t.FullName.ToLower().Contains(kw)).ToList();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; dgTeachers.ItemsSource = _all; }
    private void DgTeachers_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnAdd_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Add teacher — TODO");
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgTeachers.SelectedItem is not Teacher t) { MessageBox.Show("Please select a teacher."); return; }
        MessageBox.Show($"Edit: {t.FullName} — TODO");
    }
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgTeachers.SelectedItem is not Teacher t) { MessageBox.Show("Please select a teacher."); return; }
        var confirm = MessageBox.Show($"Delete {t.FullName}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(t.TeacherId); LoadData(); }
    }
}
