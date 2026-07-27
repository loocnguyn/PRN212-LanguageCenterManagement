using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// NOTE: SemesterFilterItem and CourseFilterItem are already defined in
// AttendanceHistoryWindow.xaml.cs (same "WpfApp" namespace) — reused here,
// do NOT redeclare them in this file.

public class ClassDisplayItem
{
    public int SemesterId { get; set; }
    public int CourseId { get; set; }
    public string SemesterName { get; set; } = "";
    public string CourseDisplay { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string ClassroomName { get; set; } = "";
    public string DateRangeDisplay { get; set; } = "";
    public string ClassStatus { get; set; } = "";
    public string EnrollmentStatus { get; set; } = "";
    public DateOnly? SemesterStartDate { get; set; }
}

// ============================================================
//  MyClassesWindow — the student's enrolled classes.
//  CONTENTS:
//    1. Construction & LoadClasses — the student's enrollments
//    2. Filters                    — semester/course/show-all -> display
//    3. Helpers                    — date-range formatting
// ============================================================
public partial class MyClassesWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();

    private int _studentId;
    private List<Enrollment> _allEnrollments = new();

    public MyClassesWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadClasses();
    }

    private void LoadClasses()
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
                tbNoClasses.Visibility = Visibility.Collapsed;
                return;
            }
            _studentId = student.StudentId;
            tbStudentInfo.Text = student.FullName;

            // Load all enrollments for this student
            _allEnrollments = _enrollmentService.GetByStudentId(_studentId);

            if (!_allEnrollments.Any())
            {
                cbSemester.Visibility = Visibility.Collapsed;
                cbCourse.Visibility = Visibility.Collapsed;
                dgClasses.ItemsSource = null;
                tbNoClasses.Visibility = Visibility.Visible;
                tbSummary.Text = "";
                return;
            }

            cbSemester.Visibility = Visibility.Visible;
            cbCourse.Visibility = Visibility.Visible;

            var semesterItems = new List<SemesterFilterItem> { new() { SemesterId = 0, DisplayName = "All Semesters" } };
            semesterItems.AddRange(_allEnrollments
                .Where(en => en.Class?.Semester != null)
                .Select(en => en.Class.Semester)
                .GroupBy(s => s.SemesterId)
                .Select(g => g.First())
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SemesterFilterItem { SemesterId = s.SemesterId, DisplayName = s.Name }));

            cbSemester.ItemsSource = semesterItems;
            cbSemester.DisplayMemberPath = "DisplayName";
            cbSemester.SelectedValuePath = "SemesterId";
            cbSemester.SelectedIndex = 0; // "All Semesters" — CbSemester_SelectionChanged fires and handles the rest.
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading classes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 1: Semester selected -> populate the Course dropdown ("All Courses" +
    /// this semester's courses, or all of the student's courses when "All Semesters" is picked).</summary>
    private void CbSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbSemester.SelectedItem is not SemesterFilterItem selected)
        {
            cbCourse.ItemsSource = null;
            dgClasses.ItemsSource = null;
            return;
        }

        var enrollmentsInScope = selected.SemesterId == 0
            ? _allEnrollments
            : _allEnrollments.Where(en => en.Class?.SemesterId == selected.SemesterId).ToList();

        var courseItems = new List<CourseFilterItem> { new() { CourseId = 0, DisplayName = "All Courses" } };
        courseItems.AddRange(enrollmentsInScope
            .Where(en => en.Class?.Course != null)
            .Select(en => en.Class.Course)
            .GroupBy(c => c.CourseId)
            .Select(g => g.First())
            .OrderBy(c => c.Name)
            .Select(c => new CourseFilterItem { CourseId = c.CourseId, DisplayName = c.Name }));

        cbCourse.ItemsSource = courseItems;
        cbCourse.DisplayMemberPath = "DisplayName";
        cbCourse.SelectedValuePath = "CourseId";
        cbCourse.SelectedIndex = 0; // "All Courses" — CbCourse_SelectionChanged fires and refreshes the grid.
    }

    /// <summary>Step 2: Course selected -> refresh the class list.</summary>
    private void CbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshDisplay();
    }

    private void CbShowAll_Changed(object sender, RoutedEventArgs e)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        try
        {
            var displayEnrollments = _allEnrollments;

            if (cbSemester.SelectedItem is SemesterFilterItem selectedSemester && selectedSemester.SemesterId != 0)
                displayEnrollments = displayEnrollments.Where(en => en.Class?.SemesterId == selectedSemester.SemesterId).ToList();

            if (cbCourse.SelectedItem is CourseFilterItem selectedCourse && selectedCourse.CourseId != 0)
                displayEnrollments = displayEnrollments.Where(en => en.Class?.CourseId == selectedCourse.CourseId).ToList();

            // Filter based on checkbox
            if (!cbShowAll.IsChecked.GetValueOrDefault(false))
            {
                displayEnrollments = displayEnrollments.Where(en => en.Status == "ACTIVE").ToList();
            }

            if (!displayEnrollments.Any())
            {
                dgClasses.ItemsSource = null;
                tbNoClasses.Text = cbShowAll.IsChecked.GetValueOrDefault(false)
                    ? "No class matches this filter."
                    : "No class you are currently taking matches this filter.\nTick \"Include finished and dropped\" to see the rest.";
                tbNoClasses.Visibility = Visibility.Visible;
                tbSummary.Text = "";
                return;
            }

            // Create display items
            var displayItems = displayEnrollments
                .Select(en => new ClassDisplayItem
                {
                    SemesterId = en.Class?.SemesterId ?? 0,
                    CourseId = en.Class?.CourseId ?? 0,
                    SemesterName = en.Class?.Semester?.Name ?? "N/A",
                    CourseDisplay = $"{en.Class?.Course?.Name ?? "N/A"} ({en.Class?.Course?.Code ?? "N/A"})",
                    ClassName = en.Class?.Name ?? "N/A",
                    TeacherName = en.Class?.PrimaryTeacher?.FullName ?? "N/A",
                    ClassroomName = en.Class?.Classroom?.Name ?? "N/A",
                    DateRangeDisplay = FormatDateRange(en.Class?.StartDate, en.Class?.EndDate),
                    ClassStatus = en.Class?.Status ?? "N/A",
                    EnrollmentStatus = en.Status ?? "N/A",
                    SemesterStartDate = en.Class?.Semester?.StartDate
                })
                .ToList();

            // Sort: Semester descending by StartDate, then Class name ascending
            // Handle null SemesterStartDate by placing those items at the end
            displayItems = displayItems
                .OrderByDescending(item => item.SemesterStartDate ?? DateOnly.MinValue)
                .ThenBy(item => item.ClassName)
                .ToList();

            dgClasses.ItemsSource = displayItems;
            tbNoClasses.Visibility = Visibility.Collapsed;
            var ongoing = displayItems.Count(i => i.ClassStatus == "ONGOING");
            tbSummary.Text = $"{displayItems.Count} class(es) shown · {ongoing} running right now";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string FormatDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate == null || endDate == null)
            return "TBD";

        return $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
    }
}
