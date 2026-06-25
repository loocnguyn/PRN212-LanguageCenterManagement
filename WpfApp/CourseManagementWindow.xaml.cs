using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class CourseManagementWindow : Window
{
    private readonly ICourseService _service = new CourseService();
    private List<Course> _all = new();

    public CourseManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData() { _all = _service.GetAll(); dgCourses.ItemsSource = _all; }
    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgCourses.ItemsSource = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(c => c.Name.ToLower().Contains(kw) || c.Code.ToLower().Contains(kw)).ToList();
    }
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; dgCourses.ItemsSource = _all; }
    private void DgCourses_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void BtnAdd_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Add course — TODO");
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgCourses.SelectedItem is not Course c) { MessageBox.Show("Please select a course."); return; }
        MessageBox.Show($"Edit: {c.Name} — TODO");
    }
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgCourses.SelectedItem is not Course c) { MessageBox.Show("Please select a course."); return; }
        var confirm = MessageBox.Show($"Delete {c.Name}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(c.CourseId); LoadData(); }
    }
}
