using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassScheduleEditorWindow : Window
{
    private readonly IClassScheduleService _service = new ClassScheduleService();
    private readonly int _classId;

    public ClassScheduleEditorWindow(int classId, string className)
    {
        InitializeComponent();
        _classId = classId;
        tbClassName.Text = className;
        LoadData();
    }

    private void LoadData()
    {
        dgSchedules.ItemsSource = _service.GetAll()
            .Where(s => s.ClassId == _classId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new Row(s))
            .ToList();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ClassScheduleDialog(_classId) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _service.Save(dialog.Result);
            LoadData();
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgSchedules.SelectedItem is not Row r)
        { MessageBox.Show("Please select a session to delete."); return; }
        var confirm = MessageBox.Show($"Delete the {r.DayName} {r.StartTimeStr}–{r.EndTimeStr} session?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(r.Schedule.ScheduleId); LoadData(); }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private record Row(ClassSchedule Schedule)
    {
        public string DayName => Schedule.DayOfWeek switch
        {
            1 => "Monday", 2 => "Tuesday", 3 => "Wednesday", 4 => "Thursday",
            5 => "Friday", 6 => "Saturday", 7 => "Sunday", _ => "Unknown"
        };
        public string StartTimeStr => Schedule.StartTime.ToString("HH\\:mm");
        public string EndTimeStr => Schedule.EndTime.ToString("HH\\:mm");
    }
}
