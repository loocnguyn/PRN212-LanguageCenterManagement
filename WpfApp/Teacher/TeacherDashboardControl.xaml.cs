using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

// TeacherDashboardControl — teacher home: hero + class & upcoming-session tiles (RefreshData).
public partial class TeacherDashboardControl : UserControl, IDashboardControl
{
    private readonly User _currentUser;
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISessionService _sessionService = new SessionService();

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
                : _classService.GetClassesForTeacher(teacher.TeacherId, semester.SemesterId);

            var classIds = myClasses.Select(c => c.ClassId).ToList();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcomingSessions = _sessionService.GetByClassIds(classIds)
                .Count(s => s.SessionDate >= today && s.SessionDate <= today.AddDays(7));

            panelTiles.Children.Clear();
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Classes This Semester", myClasses.Count.ToString(), "ClipboardTask24", FindBrush("PrimaryBrush")));
            panelTiles.Children.Add(DashboardTileBuilder.BuildTile(
                "Upcoming Sessions (7d)", upcomingSessions.ToString(), "CalendarLtr24", FindBrush("SecondaryBrush")));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Brush FindBrush(string key) => (Brush)Application.Current.Resources[key];
}
