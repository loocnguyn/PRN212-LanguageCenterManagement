using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;
using System.Windows.Data;

namespace WpfApp;

public partial class TeacherScheduleWindow : Window
{
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();

    public TeacherScheduleWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(txtTeacherId.Text.Trim(), out int teacherId))
            {
                MessageBox.Show("Please enter a valid Teacher ID.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var teacher = _teacherService.GetById(teacherId)
                ?? throw new InvalidOperationException($"Teacher {teacherId} not found.");

            var semester = _semesterService.GetActive()
                ?? throw new InvalidOperationException("No active semester.");

            tbTeacherName.Text = $"Teacher: {teacher.FullName}";
            tbSemesterInfo.Text = $"Semester: {semester.Name}";

            // Generate sessions if in LEARNING phase
            var phase = _semesterService.GetPhase(semester);
            if (phase == Phase.LEARNING)
                _sessionService.EnsureSessionsForSemester(semester.SemesterId);

            // Get classes for this teacher in active semester
            var classes = _classService.GetBySemesterId(semester.SemesterId)
                .Where(c => c.TeacherId == teacherId)
                .ToList();

            if (!classes.Any())
            {
                dgSchedule.ItemsSource = null;
                tbSummary.Text = "No classes found for this teacher in the active semester.";
                return;
            }

            var classIds = classes.Select(c => c.ClassId).ToList();
            var sessions = _sessionService.GetByClassIds(classIds);

            var displayItems = sessions.Select(s => new ScheduleDisplay
            {
                SessionDate = s.SessionDate,
                DayName = s.SessionDate.DayOfWeek.ToString(),
                ClassName = s.Class?.Name ?? "",
                TimeDisplay = s.Schedule != null
                    ? $"{s.Schedule.StartTime:hh\\:mm} - {s.Schedule.EndTime:hh\\:mm}"
                    : "",
                RoomName = s.Class?.Classroom?.Name ?? "",
                CourseName = s.Class?.Course?.Name ?? "",
                Status = s.Status
            })
            .OrderBy(d => d.SessionDate)
            .ThenBy(d => d.TimeDisplay)
            .ToList();

            // Client-side time ordering since EF may not translate cross-navigation ternary

            var cvs = (CollectionViewSource)FindResource("ScheduleViewSource");
            cvs.Source = displayItems;
            dgSchedule.ItemsSource = cvs.View;
            tbSummary.Text = $"{displayItems.Count} session(s) across {classes.Count} class(es)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
