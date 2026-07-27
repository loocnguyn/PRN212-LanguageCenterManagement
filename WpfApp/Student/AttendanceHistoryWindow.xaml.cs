using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// SemesterFilterItem and CourseFilterItem are defined once here and reused
// (same "WpfApp" namespace) by StudentGradeWindow.xaml.cs and MyClassesWindow.xaml.cs
// — do NOT redeclare them in those files.
public class SemesterFilterItem
{
    public int SemesterId { get; set; }
    public string DisplayName { get; set; } = "";
}

// CourseId == 0 is the sentinel "All Courses" option.
public class CourseFilterItem
{
    public int CourseId { get; set; }
    public string DisplayName { get; set; } = "";
}

public class ClassAttendanceSummaryItem
{
    public int ClassId { get; set; }
    public int CourseId { get; set; }
    public string CourseDisplay { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public int TotalSessions { get; set; }
    public int AbsentCount { get; set; }
}

// ============================================================
//  AttendanceHistoryWindow — student's attendance across classes.
//  CONTENTS:
//    1. Construction & LoadAttendance — pull the student's records
//    2. Semester/course filters       — cascading combos -> class list
//    3. Row actions                   — double-click for class detail
// ============================================================
public partial class AttendanceHistoryWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IAttendanceService _attendanceService = new AttendanceService();

    private int _studentId;
    private List<Attendance> _allAttendances = new();

    public AttendanceHistoryWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadAttendance();
    }

    private void LoadAttendance()
    {
        try
        {
            var student = _studentService.GetByUserId(_currentUser.Id);
            if (student == null)
            {
                tbStudentInfo.Text = "No student profile linked to this account.";
                cbSemester.Visibility = Visibility.Collapsed;
                cbCourse.Visibility = Visibility.Collapsed;
                dgClasses.ItemsSource = null;
                tbNoData.Visibility = Visibility.Collapsed;
                return;
            }
            _studentId = student.StudentId;
            tbStudentInfo.Text = $"Student: {student.FullName}";

            _allAttendances = _attendanceService.GetByStudentId(_studentId);

            if (!_allAttendances.Any())
            {
                cbSemester.Visibility = Visibility.Collapsed;
                cbCourse.Visibility = Visibility.Collapsed;
                dgClasses.ItemsSource = null;
                tbNoData.Text = "You have no attendance records yet.";
                tbNoData.Visibility = Visibility.Visible;
                return;
            }

            cbSemester.Visibility = Visibility.Visible;
            cbCourse.Visibility = Visibility.Visible;
            tbNoData.Visibility = Visibility.Collapsed;

            var semesterItems = _allAttendances
                .Where(a => a.Session?.Class?.Semester != null)
                .Select(a => a.Session.Class.Semester)
                .GroupBy(s => s.SemesterId)
                .Select(g => g.First())
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SemesterFilterItem { SemesterId = s.SemesterId, DisplayName = s.Name })
                .ToList();

            cbSemester.ItemsSource = semesterItems;
            cbSemester.DisplayMemberPath = "DisplayName";
            cbSemester.SelectedValuePath = "SemesterId";

            if (semesterItems.Count > 0)
            {
                // Prefer the semester whose date range contains today; otherwise the most recent one.
                var today = DateOnly.FromDateTime(DateTime.Now);
                var currentSemester = _allAttendances
                    .Select(a => a.Session.Class.Semester)
                    .FirstOrDefault(s => s != null && s.StartDate <= today && s.EndDate >= today);

                cbSemester.SelectedIndex = 0;
                if (currentSemester != null)
                {
                    var match = semesterItems.FirstOrDefault(x => x.SemesterId == currentSemester.SemesterId);
                    if (match != null) cbSemester.SelectedItem = match;
                }
            }

            // CboSemester_SelectionChanged fires from the assignment above and handles the rest.
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading attendance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 1: Semester selected -> populate the Course dropdown ("All Courses" + this
    /// semester's courses) then refresh the class list.</summary>
    private void CbSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbSemester.SelectedItem is not SemesterFilterItem selected)
        {
            cbCourse.ItemsSource = null;
            dgClasses.ItemsSource = null;
            return;
        }

        var courseItems = new List<CourseFilterItem> { new() { CourseId = 0, DisplayName = "All Courses" } };
        courseItems.AddRange(_allAttendances
            .Where(a => a.Session?.Class?.SemesterId == selected.SemesterId && a.Session?.Class?.Course != null)
            .Select(a => a.Session.Class.Course)
            .GroupBy(c => c.CourseId)
            .Select(g => g.First())
            .OrderBy(c => c.Name)
            .Select(c => new CourseFilterItem { CourseId = c.CourseId, DisplayName = c.Name }));

        cbCourse.ItemsSource = courseItems;
        cbCourse.DisplayMemberPath = "DisplayName";
        cbCourse.SelectedValuePath = "CourseId";
        cbCourse.SelectedIndex = 0; // "All Courses" — CbCourse_SelectionChanged fires and refreshes the grid.
    }

    /// <summary>Step 2: Course selected -> refresh the class list, filtered to that course
    /// (or all courses in the semester when "All Courses" is selected).</summary>
    private void CbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshClassList();
    }

    private void RefreshClassList()
    {
        try
        {
            if (cbSemester.SelectedItem is not SemesterFilterItem selectedSemester)
            {
                dgClasses.ItemsSource = null;
                return;
            }

            var attendancesInSemester = _allAttendances
                .Where(a => a.Session?.Class?.SemesterId == selectedSemester.SemesterId)
                .ToList();

            if (cbCourse.SelectedItem is CourseFilterItem selectedCourse && selectedCourse.CourseId != 0)
            {
                attendancesInSemester = attendancesInSemester
                    .Where(a => a.Session?.Class?.CourseId == selectedCourse.CourseId)
                    .ToList();
            }

            var summaryItems = attendancesInSemester
                .GroupBy(a => a.Session.Class)
                .Select(g => new ClassAttendanceSummaryItem
                {
                    ClassId = g.Key.ClassId,
                    CourseId = g.Key.CourseId,
                    CourseDisplay = $"{g.Key.Course?.Name ?? "N/A"}",
                    ClassName = g.Key.Name,
                    TeacherName = g.Key.TeacherNames is { Length: > 0 } names ? names : "N/A",
                    TotalSessions = g.Count(),
                    AbsentCount = g.Count(a => a.Status == "ABSENT")
                })
                .OrderBy(x => x.ClassName)
                .ToList();

            dgClasses.ItemsSource = summaryItems;
            tbSummary.Text = $"Showing {summaryItems.Count} class(es)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing class list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DgClasses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgClasses.SelectedItem is not ClassAttendanceSummaryItem selected) return;

        var attendancesForClass = _allAttendances
            .Where(a => a.Session?.Class?.ClassId == selected.ClassId)
            .OrderBy(a => a.Session.SessionDate)
            .ToList();

        var detailWindow = new AttendanceDetailWindow(selected.ClassName, attendancesForClass)
        {
            Owner = this
        };
        detailWindow.ShowDialog();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadAttendance();
    }
}
