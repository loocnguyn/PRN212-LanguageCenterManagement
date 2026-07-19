using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassScheduleManagementWindow : Window
{
    private readonly IClassScheduleService _service = new ClassScheduleService();
    private readonly IClassService _classService = new ClassService();
    private List<ClassRow> _all = new();

    public ClassScheduleManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        var schedules = _service.GetAll();
        var byClass = schedules.GroupBy(s => s.ClassId).ToDictionary(g => g.Key, g => g.ToList());

        // Every class is a row, even if it has no sessions yet.
        _all = _classService.GetAll()
            .Select(c => new ClassRow(c.ClassId, c.Name,
                byClass.GetValueOrDefault(c.ClassId) ?? new List<ClassSchedule>()))
            .OrderBy(r => r.ClassName)
            .ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = txtSearch.Text.Trim().ToLower();
        dgClasses.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(r => r.ClassName.ToLower().Contains(kw)).ToList();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; ApplyFilter(); }

    private void DgClasses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => ManageSelected();

    private void BtnManage_Click(object sender, RoutedEventArgs e) => ManageSelected();

    private void ManageSelected()
    {
        if (dgClasses.SelectedItem is not ClassRow r)
        {
            MessageBox.Show("Please select a class.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new ClassScheduleEditorWindow(r.ClassId, r.ClassName) { Owner = this }.ShowDialog();
        LoadData();
    }

    private static string DayName(int d) => d switch
    {
        1 => "Mon", 2 => "Tue", 3 => "Wed", 4 => "Thu",
        5 => "Fri", 6 => "Sat", 7 => "Sun", _ => "?"
    };

    private record ClassRow(int ClassId, string ClassName, List<ClassSchedule> Schedules)
    {
        public int Count => Schedules.Count;

        public string Summary => Schedules.Count == 0
            ? "— no sessions —"
            : string.Join("   •   ", Schedules
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .Select(s => $"{DayName(s.DayOfWeek)} {s.StartTime:HH\\:mm}–{s.EndTime:HH\\:mm}"));
    }
}
