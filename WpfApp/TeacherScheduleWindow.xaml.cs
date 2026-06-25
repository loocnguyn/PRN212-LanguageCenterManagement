using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class TeacherScheduleWindow : Window
{
    private readonly IClassScheduleService _service = new ClassScheduleService();

    public TeacherScheduleWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Load teacher schedule — TODO");

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtTeacherId.Text = ""; dgSchedule.ItemsSource = null; }
}
