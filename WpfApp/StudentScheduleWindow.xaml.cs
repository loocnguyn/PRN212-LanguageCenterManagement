using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class StudentScheduleWindow : Window
{
    private readonly IClassScheduleService _service = new ClassScheduleService();

    public StudentScheduleWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        // TODO: query schedules for the given student via enrollment → class → schedule
        MessageBox.Show("Load schedule for student — TODO");
    }
}
