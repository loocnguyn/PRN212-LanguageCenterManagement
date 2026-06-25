using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class ClassRosterWindow : Window
{
    private readonly IEnrollmentService _service = new EnrollmentService();

    public ClassRosterWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        // TODO: load students enrolled in classId, bind to dgRoster
        MessageBox.Show("Load class roster — TODO");
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtClassId.Text = ""; dgRoster.ItemsSource = null; }
}
