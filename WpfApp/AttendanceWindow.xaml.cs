using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class AttendanceWindow : Window
{
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IAttendanceService _attendanceService = new AttendanceService();

    private List<Class> _teacherClasses = new();
    private List<Session> _classSessions = new();
    private Semester? _activeSemester;

    public AttendanceWindow() { InitializeComponent(); }

    private void BtnLoadClasses_Click(object sender, RoutedEventArgs e)
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

            _activeSemester = _semesterService.GetActive()
                ?? throw new InvalidOperationException("No active semester.");

            tbTeacherName.Text = $"Teacher: {teacher.FullName}";
            tbSemesterInfo.Text = $"Semester: {_activeSemester.Name}";

            _teacherClasses = _classService.GetBySemesterId(_activeSemester.SemesterId)
                .Where(c => c.TeacherId == teacherId)
                .ToList();

            cboClass.ItemsSource = _teacherClasses;
            cboSession.ItemsSource = null;
            dgAttendance.ItemsSource = null;

            if (!_teacherClasses.Any())
                tbSummary.Text = "No classes found for this teacher in the active semester.";
            else
                tbSummary.Text = $"{_teacherClasses.Count} class(es) loaded. Select a class.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls) return;

        try
        {
            _classSessions = _sessionService.GetByClassIdWithDetails(cls.ClassId)
                .OrderBy(s => s.SessionDate)
                .ToList();

            var displaySessions = _classSessions.Select(s => new SessionDisplayItem
            {
                Session = s,
                SessionDisplay = $"{s.SessionDate:dd/MM/yyyy} ({s.SessionDate.DayOfWeek}) - {s.Status}"
            }).ToList();

            cboSession.ItemsSource = displaySessions;
            cboSession.SelectedIndex = -1;
            dgAttendance.ItemsSource = null;
            tbSummary.Text = $"Class '{cls.Name}': {_classSessions.Count} session(s). Select a session date.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CboSession_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboSession.SelectedItem is not SessionDisplayItem item) return;

        try
        {
            var session = item.Session;
            var classId = session.ClassId;

            // Get enrolled students
            var enrollments = _enrollmentService.GetByClassId(classId);
            // Get existing attendance records
            var existingAttendance = _attendanceService.GetBySessionId(session.SessionId);

            var attendanceRows = enrollments.Select(en =>
            {
                var existing = existingAttendance.FirstOrDefault(a => a.StudentId == en.StudentId);
                return new AttendanceRow
                {
                    StudentId = en.StudentId,
                    StudentName = en.Student?.FullName ?? "",
                    Status = existing?.Status ?? "PRESENT",
                    Note = existing?.Note ?? "",
                    AttendanceId = existing?.AttendanceId
                };
            }).ToList();

            dgAttendance.ItemsSource = attendanceRows;
            tbSummary.Text = $"Session {session.SessionDate:dd/MM/yyyy}: {attendanceRows.Count} student(s).";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading students: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (cboSession.SelectedItem is not SessionDisplayItem item)
        {
            MessageBox.Show("Please select a session date first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (dgAttendance.ItemsSource is not List<AttendanceRow> rows || !rows.Any())
        {
            MessageBox.Show("No attendance data to save.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            foreach (var row in rows)
            {
                var attendance = new Attendance
                {
                    AttendanceId = row.AttendanceId ?? 0,
                    SessionId = item.Session.SessionId,
                    StudentId = row.StudentId,
                    Status = row.Status,
                    Note = row.Note,
                    RecordedAt = DateTime.Now
                };
                _attendanceService.Upsert(attendance);
            }
            MessageBox.Show($"Attendance saved for {rows.Count} student(s).", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving attendance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class SessionDisplayItem
{
    public Session Session { get; set; } = null!;
    public string SessionDisplay { get; set; } = "";
}

public class AttendanceRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string Status { get; set; } = "PRESENT";
    public string Note { get; set; } = "";
    public int? AttendanceId { get; set; }
}
