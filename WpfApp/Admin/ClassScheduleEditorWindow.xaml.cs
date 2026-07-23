using System.Windows;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ClassScheduleEditorWindow — manage one class's weekly pattern.
//  CONTENTS:
//    1. Construction & LoadData — that class's schedule rows
//    2. Coverage                — meetings delivered vs. required
//    3. Add / Delete            — ClassScheduleDialog (class locked); delete
//    4. Close guard             — refuse to leave an under-scheduled class
//    5. Row                     — grid-facing view model
//
//  The course decides how many meetings a class runs (frozen onto the class as
//  SnapDurationSessions); the weekly pattern here decides when they happen. A
//  pattern that cannot fit them all inside the semester would silently produce a
//  short class, so leaving in that state is blocked.
// ============================================================
public partial class ClassScheduleEditorWindow : Window
{
    private readonly IClassScheduleService _service = new ClassScheduleService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IClassService _classService = new ClassService();

    private readonly int _classId;
    private Class? _class;

    /// <summary>Meetings the current pattern delivers, and how many the course wants.</summary>
    private int _available;
    private int _required;

    private static readonly Brush AmberBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0x80, 0x00));

    public ClassScheduleEditorWindow(int classId, string className)
    {
        InitializeComponent();
        _classId = classId;
        tbClassName.Text = className;
        _class = _classService.GetById(classId);
        LoadData();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadData()
    {
        dgSchedules.ItemsSource = _service.GetAll()
            .Where(s => s.ClassId == _classId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new Row(s))
            .ToList();

        UpdateCoverage();
    }

    // ---- 2. Coverage -------------------------------------------
    private void UpdateCoverage()
    {
        _required = _class?.SnapDurationSessions ?? 0;

        try
        {
            _available = _sessionService.GetAvailableSessionDates(_classId).Count;
        }
        catch (Exception ex)
        {
            // Missing class/semester — report it rather than showing a misleading zero.
            tbCoverage.Text = "unavailable";
            tbCoverage.Foreground = (Brush)FindResource("DangerBrush");
            tbCoverageHint.Text = $"Could not work out the schedule: {ex.Message}";
            _available = 0;
            return;
        }

        tbCoverage.Text = $"{_available} / {_required}";

        if (_required <= 0)
        {
            tbCoverage.Foreground = (Brush)FindResource("TextSecondaryBrush");
            tbCoverageHint.Text = "This class has no session count recorded, so nothing to check against.";
        }
        else if (_available >= _required)
        {
            tbCoverage.Foreground = (Brush)FindResource("SecondaryBrush");
            tbCoverageHint.Text = _available == _required
                ? "The weekly pattern fits the course exactly."
                : $"The pattern could run {_available} meetings; the first {_required} will be scheduled.";
        }
        else
        {
            tbCoverage.Foreground = (Brush)FindResource("DangerBrush");
            tbCoverageHint.Text =
                $"{_required - _available} meeting(s) short. Add another weekly slot — "
                + "or, if the semester is too short for this course, widen the semester dates.";
        }
    }

    // ---- 3. Add / Delete ---------------------------------------
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

    // ---- 4. Close guard ----------------------------------------
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel) return;

        // Nothing to enforce when the class records no session count.
        if (_required <= 0 || _available >= _required) return;

        var answer = MessageBox.Show(
            $"This schedule only covers {_available} of the {_required} sessions "
            + $"\"{_class?.SnapCourseName}\" requires.\n\n"
            + "Leaving now means the class will be generated short.\n\n"
            + "Stay and fix the schedule?",
            "Not enough sessions",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        // Yes = stay. No is deliberately an escape hatch: if the semester is simply
        // too short for the course, no weekly pattern can satisfy this, and trapping
        // the admin in the window would leave them no way out.
        if (answer == MessageBoxResult.Yes) e.Cancel = true;
    }

    // ---- 5. Row ------------------------------------------------
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
