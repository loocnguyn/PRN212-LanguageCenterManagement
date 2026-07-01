using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassScheduleManagementWindow : Window
{
    private readonly IClassScheduleService _service = new ClassScheduleService();
    private readonly IClassService _classService = new ClassService();
    private List<ClassSchedule> _all = new();
    private List<ScheduleDisplay> _displayAll = new();

    public ClassScheduleManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        _all = _service.GetAll();
        var classes = _classService.GetAll();
        var classDict = classes.ToDictionary(c => c.ClassId, c => c.Name);
        cboClassFilter.ItemsSource = classes;
        _displayAll = _all.Select(s => new ScheduleDisplay(s, classDict.GetValueOrDefault(s.ClassId, "?"))).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _displayAll.AsEnumerable();
        if (cboClassFilter.SelectedValue is int classId)
            filtered = filtered.Where(d => d.Schedule.ClassId == classId);
        var kw = txtSearch.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(kw))
            filtered = filtered.Where(d => d.DayName.ToLower().Contains(kw) || d.ClassName.ToLower().Contains(kw));
        dgSchedules.ItemsSource = filtered.ToList();
    }

    private void CboClassFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; cboClassFilter.SelectedIndex = -1; ApplyFilter(); }
    private void DgSchedules_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ClassScheduleDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _service.Save(dialog.Result);
            LoadData();
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgSchedules.SelectedItem is not ScheduleDisplay d)
        { MessageBox.Show("Please select a schedule."); return; }
        var dialog = new ClassScheduleDialog(d.Schedule);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _service.Update(dialog.Result);
            LoadData();
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgSchedules.SelectedItem is not ScheduleDisplay d)
        { MessageBox.Show("Please select a schedule."); return; }
        var confirm = MessageBox.Show($"Delete schedule #{d.Schedule.ScheduleId}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(d.Schedule.ScheduleId); LoadData(); }
    }

    private record ScheduleDisplay(ClassSchedule Schedule, string ClassName)
    {
        public int ScheduleId => Schedule.ScheduleId;
        public string DayName => Schedule.DayOfWeek switch
        {
            1 => "Monday", 2 => "Tuesday", 3 => "Wednesday", 4 => "Thursday",
            5 => "Friday", 6 => "Saturday", 7 => "Sunday", _ => "Unknown"
        };
        public string StartTimeStr => Schedule.StartTime.ToString("HH:mm");
        public string EndTimeStr => Schedule.EndTime.ToString("HH:mm");
    }
}
