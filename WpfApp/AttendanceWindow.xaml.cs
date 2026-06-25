using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class AttendanceWindow : Window
{
    private readonly IAttendanceService _service = new AttendanceService();

    public AttendanceWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        // TODO: load students for the session, bind to dgAttendance
        MessageBox.Show("Load attendance list for session — TODO");
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtSessionId.Text = "";
        dgAttendance.ItemsSource = null;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // TODO: persist updated attendance rows via _service
        MessageBox.Show("Save attendance — TODO");
    }
}
