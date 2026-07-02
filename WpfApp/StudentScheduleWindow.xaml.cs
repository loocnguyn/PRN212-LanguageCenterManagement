using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;
using System.Windows.Data;

namespace WpfApp;

public partial class StudentScheduleWindow : Window
{
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();

    public StudentScheduleWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(txtStudentId.Text.Trim(), out int studentId))
            {
                MessageBox.Show("Please enter a valid Student ID.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = _studentService.GetById(studentId)
                ?? throw new InvalidOperationException($"Student {studentId} not found.");

            var semester = _semesterService.GetActive()
                ?? throw new InvalidOperationException("No active semester.");

            tbStudentName.Text = $"Student: {student.FullName}";
            tbSemesterInfo.Text = $"Semester: {semester.Name}";

            // Get active enrollments for student in active semester
            var allEnrollments = _enrollmentService.GetByStudentId(studentId);
            var semesterEnrollments = allEnrollments
                .Where(en => en.Class?.SemesterId == semester.SemesterId)
                .ToList();

            if (!semesterEnrollments.Any())
            {
                dgSchedule.ItemsSource = null;
                tbSummary.Text = "No active enrollments in the current semester.";
                return;
            }

            var classIds = semesterEnrollments.Select(en => en.ClassId).ToList();
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
                TeacherName = s.Class?.Teacher?.FullName ?? "",
                Status = s.Status
            })
            .OrderBy(d => d.SessionDate)
            .ThenBy(d => d.TimeDisplay)
            .ToList();

            var cvs = (CollectionViewSource)FindResource("ScheduleViewSource");
            cvs.Source = displayItems;
            dgSchedule.ItemsSource = cvs.View;
            tbSummary.Text = $"{displayItems.Count} session(s) across {semesterEnrollments.Count} class(es)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
