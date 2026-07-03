using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassScheduleDialog : Window
{
    public ClassSchedule? Result { get; private set; }
    private readonly int? _editId;
    private readonly IClassService _classService = new ClassService();
    private readonly IClassScheduleService _scheduleService = new ClassScheduleService();

    private static readonly List<KeyValuePair<string, byte>> Days = new()
    {
        new("Monday", 1), new("Tuesday", 2), new("Wednesday", 3),
        new("Thursday", 4), new("Friday", 5), new("Saturday", 6), new("Sunday", 7)
    };

    public ClassScheduleDialog()
    {
        InitializeComponent();
        LoadDropdowns();
    }

    public ClassScheduleDialog(ClassSchedule schedule)
    {
        InitializeComponent();
        _editId = schedule.ScheduleId;
        LoadDropdowns();
        cboClass.SelectedValue = schedule.ClassId;
        cboDayOfWeek.SelectedValue = schedule.DayOfWeek;
        txtStartTime.Text = schedule.StartTime.ToString("HH:mm");
        txtEndTime.Text = schedule.EndTime.ToString("HH:mm");
    }

    private void LoadDropdowns()
    {
        cboClass.ItemsSource = _classService.GetAll();
        cboDayOfWeek.ItemsSource = Days;
    }

    private string? Validate()
    {
        if (cboClass.SelectedValue == null)
            return "Please select a Class.";
        if (cboDayOfWeek.SelectedValue == null)
            return "Please select a Day of Week.";
        if (!TimeOnly.TryParse(txtStartTime.Text.Trim(), out _))
            return "Start Time must be in HH:mm format (e.g. 08:00).";
        if (!TimeOnly.TryParse(txtEndTime.Text.Trim(), out _))
            return "End Time must be in HH:mm format (e.g. 10:00).";
        var start = TimeOnly.Parse(txtStartTime.Text.Trim());
        var end = TimeOnly.Parse(txtEndTime.Text.Trim());
        if (end <= start)
            return "End Time must be after Start Time.";
        return null;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error != null) { MessageBox.Show(error, "Validation"); return; }

        var schedule = new ClassSchedule
        {
            ScheduleId = _editId ?? 0,
            ClassId = (int)cboClass.SelectedValue,
            DayOfWeek = (byte)cboDayOfWeek.SelectedValue,
            StartTime = TimeOnly.Parse(txtStartTime.Text.Trim()),
            EndTime = TimeOnly.Parse(txtEndTime.Text.Trim())
        };

        var conflicts = _scheduleService.CheckConflicts(schedule);
        if (conflicts.Count > 0)
        {
            var msg = "Conflict detected:\n\n" + string.Join("\n", conflicts);
            MessageBox.Show(msg, "Conflict Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = schedule;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
