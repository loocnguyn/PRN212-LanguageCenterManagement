using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class AttendanceHistoryWindow : Window
{
    private readonly IAttendanceService _service = new AttendanceService();

    public AttendanceHistoryWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Load attendance history — TODO");

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtStudentId.Text = ""; dgHistory.ItemsSource = null; }
}
