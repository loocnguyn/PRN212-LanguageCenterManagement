using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class EnrollmentWindow : Window
{
    private readonly IEnrollmentService _service = new EnrollmentService();
    private List<Enrollment> _all = new();

    public EnrollmentWindow() { InitializeComponent(); LoadData(); }

    private void LoadData() { _all = _service.GetAll(); dgEnrollments.ItemsSource = _all; }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var sid = txtStudentId.Text.Trim();
        var cid = txtClassId.Text.Trim();
        dgEnrollments.ItemsSource = _all
            .Where(x => (string.IsNullOrEmpty(sid) || x.StudentId.ToString() == sid)
                     && (string.IsNullOrEmpty(cid)  || x.ClassId.ToString()   == cid))
            .ToList();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtStudentId.Text = "";
        txtClassId.Text   = "";
        dgEnrollments.ItemsSource = _all;
    }

    private void DgEnrollments_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnEnroll_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Enroll student into class — TODO");

    private void BtnDrop_Click(object sender, RoutedEventArgs e)
    {
        if (dgEnrollments.SelectedItem is not Enrollment en) { MessageBox.Show("Please select an enrollment."); return; }
        var confirm = MessageBox.Show($"Drop enrollment #{en.EnrollmentId}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(en.EnrollmentId); LoadData(); }
    }
}
