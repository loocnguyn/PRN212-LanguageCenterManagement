using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class TeacherDashboardControl : UserControl, IDashboardControl
{
    private readonly User _currentUser;
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeService _gradeService = new GradeService();

    public TeacherDashboardControl(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => RefreshData();
    }

    public void RefreshData()
    {
        tbSubHeader.Text = "Your classes and teaching overview";

        try
        {
            var teacher = _teacherService.GetByUserId(_currentUser.Id);
            if (teacher == null)
            {
                MessageBox.Show("Teacher profile not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            tbHeader.Text = $"Welcome back, {teacher.FullName}";

            var semester = _semesterService.GetActive();
            var myClasses = semester == null
                ? new List<Class>()
                : _classService.GetBySemesterId(semester.SemesterId)
                    .Where(c => c.TeacherId == teacher.TeacherId)
                    .ToList();

            var classIds = myClasses.Select(c => c.ClassId).ToList();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var mySessions = _sessionService.GetByClassIds(classIds);
            var upcomingSessions = mySessions
                .Count(s => s.SessionDate >= today && s.SessionDate <= today.AddDays(7));

            var attendances = mySessions
                .SelectMany(s => s.TeacherAttendances)
                .ToList();
            var attendanceRate = attendances.Count == 0
                ? 0
                : attendances.Count(a => a.Status == "PRESENT") * 100.0 / attendances.Count;

            var classesWithMissingGrades = new List<string>();
            foreach (var cls in myClasses)
            {
                var enrolledCount = _enrollmentService.GetAll().Count(e => e.ClassId == cls.ClassId);
                var gradedCount = _gradeService.GetAll()
                    .Where(g => g.Enrollment.ClassId == cls.ClassId)
                    .Select(g => g.Enrollment.StudentId)
                    .Distinct()
                    .Count();
                if (enrolledCount > 0 && gradedCount < enrolledCount)
                    classesWithMissingGrades.Add($"{cls.Name}: {gradedCount}/{enrolledCount} students graded");
            }

            panelTiles.Children.Clear();
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Classes This Semester", myClasses.Count.ToString(), "ClipboardTask24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Upcoming Sessions (7d)", upcomingSessions.ToString(), "CalendarLtr24", FindBrush("SecondaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Avg Attendance Rate", attendanceRate.ToString("F0") + "%", "PeopleCheckmark24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Classes Missing Grades", classesWithMissingGrades.Count.ToString(), "BookStar24", FindBrush("DangerBrush"),
                classesWithMissingGrades.Count > 0 ? "Not all students graded" : null));

            lstWarnings.ItemsSource = classesWithMissingGrades.Count > 0
                ? classesWithMissingGrades
                : new List<string> { "No warnings — all classes fully graded." };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Brush FindBrush(string key) => (Brush)Application.Current.Resources[key];
}
