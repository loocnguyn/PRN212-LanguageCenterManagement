using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class StudentGradeWindow : Window
{
    private readonly IGradeService _service = new GradeService();

    public StudentGradeWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Load student grades — TODO");

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtStudentId.Text = ""; dgGrades.ItemsSource = null; }
}
