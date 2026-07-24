using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  AttendanceWindow — teacher marks attendance for a session.
//  CONTENTS:
//    1. Construction & LoadTeacherData — the teacher's classes
//    2. Cascading selects              — semester->course->class->session
//    3. Save                           — persist the roster's attendance
// ============================================================
public partial class AttendanceWindow : Window
{
    private readonly User _currentUser;
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IAttendanceService _attendanceService = new AttendanceService();

    private Teacher? _teacher;
    private List<Class> _teacherClassesInSemester = new();
    private List<Session> _classSessions = new();

    public AttendanceWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadTeacherData();
    }

    /// <summary>Step 1: load the teacher and populate the Semester dropdown.</summary>
    private void LoadTeacherData()
    {
        try
        {
            _teacher = _teacherService.GetByUserId(_currentUser.Id);
            if (_teacher == null)
            {
                tbTeacherName.Text = "No teacher profile linked to this account.";
                return;
            }

            tbTeacherName.Text = $"Teacher: {_teacher.FullName}";

            var semesters = _semesterService.GetAll()
                .OrderByDescending(s => s.StartDate)
                .ToList();

            cboSemester.ItemsSource = semesters;

            if (!semesters.Any())
            {
                tbSummary.Text = "No semesters found.";
                return;
            }

            // Default to the active semester so the common case needs no extra clicks.
            var active = semesters.FirstOrDefault(s => s.IsActive) ?? semesters.First();
            cboSemester.SelectedItem = active;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading teacher data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 2: Semester selected -> populate the Course dropdown with this
    /// teacher's courses in that semester.</summary>
    private void CboSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cboCourse.ItemsSource = null;
        cboClass.ItemsSource = null;
        cboSession.ItemsSource = null;
        dgAttendance.ItemsSource = null;
        _teacherClassesInSemester = new List<Class>();

        if (cboSemester.SelectedItem is not Semester semester || _teacher == null) return;

        try
        {
            _teacherClassesInSemester = _classService.GetClassesWithDetails(semester.SemesterId)
                .Where(c => c.ClassTeachers.Any(ct => ct.TeacherId == _teacher.TeacherId))
                .ToList();

            var courses = _teacherClassesInSemester
                .Where(c => c.Course != null)
                .Select(c => c.Course)
                .GroupBy(c => c.CourseId)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();

            cboCourse.ItemsSource = courses;

            tbSummary.Text = courses.Any()
                ? $"{semester.Name}: {courses.Count} course(s) taught. Select a course."
                : $"No classes found for this teacher in {semester.Name}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading courses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 3: Course selected -> populate the Class dropdown, filtered to
    /// classes of that course, in the chosen semester, taught by this teacher.</summary>
    private void CboCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cboClass.ItemsSource = null;
        cboSession.ItemsSource = null;
        dgAttendance.ItemsSource = null;

        if (cboCourse.SelectedItem is not Course course) return;

        var classesForCourse = _teacherClassesInSemester
            .Where(c => c.CourseId == course.CourseId)
            .ToList();

        cboClass.ItemsSource = classesForCourse;
        tbSummary.Text = classesForCourse.Any()
            ? $"{course.Name}: {classesForCourse.Count} class(es). Select a class."
            : $"No classes found for course '{course.Name}'.";
    }

    /// <summary>Step 4: Class selected -> populate the Session (date) dropdown.</summary>
    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cboSession.ItemsSource = null;
        dgAttendance.ItemsSource = null;

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
            tbSummary.Text = $"Class '{cls.Name}': {_classSessions.Count} session(s). Select a session date.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 5: Session selected -> load the attendance grid for that session.</summary>
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

            // Enforce slot-based business rules: Only allow editing if session is TODAY and CURRENT TIME falls within the slot duration
            var today = DateOnly.FromDateTime(DateTime.Today);
            bool isToday = session.SessionDate == today;
            bool isCurrentSlot = false;
            string slotTimeText = "";

            if (session.Schedule != null)
            {
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);
                isCurrentSlot = isToday && (currentTime >= session.Schedule.StartTime && currentTime <= session.Schedule.EndTime);
                slotTimeText = $"Slot: {session.Schedule.StartTime:HH\\:mm} - {session.Schedule.EndTime:HH\\:mm}";
            }
            else
            {
                slotTimeText = "No slot schedule linked";
            }

            dgAttendance.IsReadOnly = !isCurrentSlot;
            btnSave.IsEnabled = isCurrentSlot;

            if (!isCurrentSlot)
            {
                tbSummary.Text = $"Session {session.SessionDate:dd/MM/yyyy} ({slotTimeText}) [READ-ONLY] — Attendance can only be modified during its scheduled slot today.";
            }
            else
            {
                tbSummary.Text = $"Session {session.SessionDate:dd/MM/yyyy} ({slotTimeText}) [EDITABLE] — {attendanceRows.Count} student(s) enrolled.";
            }
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
