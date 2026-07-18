using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class StudentManagementWindow : Window
{
    private readonly IStudentService _service = new StudentService();
    private List<Student> _all = new();

    public StudentManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        _all = _service.GetAll();
        dgStudents.ItemsSource = _all;
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgStudents.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(s => s.FullName.ToLower().Contains(kw) || (s.Phone ?? "").Contains(kw)).ToList();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Text = "";
        dgStudents.ItemsSource = _all;
    }

    private void DgStudents_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        // TODO: open StudentDetailWindow in Add mode
        MessageBox.Show("Add student — TODO");
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgStudents.SelectedItem is not Student s) { MessageBox.Show("Please select a student."); return; }
        // TODO: open StudentDetailWindow in Edit mode, pass selected student
        MessageBox.Show($"Edit: {s.FullName} — TODO");
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgStudents.SelectedItem is not Student s) { MessageBox.Show("Please select a student."); return; }
        var confirm = MessageBox.Show($"Delete {s.FullName}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes)
        {
            _service.Delete(s.StudentId);
            LoadData();
        }
    }
}
