using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// NOTE: SemesterFilterItem and CourseFilterItem are already defined in
// AttendanceHistoryWindow.xaml.cs (same "WpfApp" namespace) — reused here,
// do NOT redeclare them in this file.

public class ClassGradeSummaryItem
{
    public int ClassId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int GradeCount { get; set; }
    public string WeightedAverageDisplay { get; set; } = "";
}

public class GradeDetailDisplayItem
{
    public string GradeTypeName { get; set; } = "";
    public string ScoreDisplay { get; set; } = "";
    public string WeightPercent { get; set; } = "";
    public string GradedAtDisplay { get; set; } = "";
}

public partial class StudentGradeWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IGradeService _gradeService = new GradeService();

    private int _studentId;
    private List<Grade> _allGrades = new();

    public StudentGradeWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadGrades();
    }

    private void LoadGrades()
    {
        try
        {
            var student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null)
            {
                tbStudentInfo.Text = "No student profile linked to this account.";
                tbSummary.Text = "";
                cbSemester.Visibility = Visibility.Collapsed;
                cbCourse.Visibility = Visibility.Collapsed;
                dgClasses.ItemsSource = null;
                return;
            }
            _studentId = student.StudentId;
            tbStudentInfo.Text = $"Student: {student.FullName}";

            _allGrades = _gradeService.GetByStudentId(_studentId);

            if (!_allGrades.Any())
            {
                tbSummary.Text = "You have no grades yet.";
                cbSemester.Visibility = Visibility.Collapsed;
                cbCourse.Visibility = Visibility.Collapsed;
                dgClasses.ItemsSource = null;
                return;
            }

            cbSemester.Visibility = Visibility.Visible;
            cbCourse.Visibility = Visibility.Visible;

            var semesterItems = _allGrades
                .Select(g => g.Enrollment.Class.Semester)
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
                var today = DateOnly.FromDateTime(DateTime.Now);
                var currentSemester = _allGrades
                    .Select(g => g.Enrollment.Class.Semester)
                    .FirstOrDefault(s => s.StartDate <= today && s.EndDate >= today);

                cbSemester.SelectedIndex = 0;
                if (currentSemester != null)
                {
                    var match = semesterItems.FirstOrDefault(x => x.SemesterId == currentSemester.SemesterId);
                    if (match != null) cbSemester.SelectedItem = match;
                }
            }

            // CbSemester_SelectionChanged fires from the assignment above and handles the rest.
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading grades: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        courseItems.AddRange(_allGrades
            .Where(g => g.Enrollment.Class.SemesterId == selected.SemesterId && g.Enrollment.Class.Course != null)
            .Select(g => g.Enrollment.Class.Course)
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

            var gradesInSemester = _allGrades
                .Where(g => g.Enrollment.Class.SemesterId == selectedSemester.SemesterId)
                .ToList();

            if (cbCourse.SelectedItem is CourseFilterItem selectedCourse && selectedCourse.CourseId != 0)
            {
                gradesInSemester = gradesInSemester
                    .Where(g => g.Enrollment.Class.CourseId == selectedCourse.CourseId)
                    .ToList();
            }

            if (!gradesInSemester.Any())
            {
                dgClasses.ItemsSource = null;
                tbSummary.Text = "No grades match this filter.";
                return;
            }

            var summaryItems = gradesInSemester
                .GroupBy(g => g.Enrollment.Class)
                .OrderBy(g => g.Key.Name)
                .Select(g => new ClassGradeSummaryItem
                {
                    ClassId = g.Key.ClassId,
                    CourseId = g.Key.CourseId,
                    CourseName = g.Key.Course?.Name ?? "N/A",
                    ClassName = g.Key.Name,
                    GradeCount = g.Count(),
                    WeightedAverageDisplay = ComputeWeightedAverageDisplay(g.ToList())
                })
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
        if (dgClasses.SelectedItem is not ClassGradeSummaryItem selected) return;

        var gradesForClass = _allGrades
            .Where(g => g.Enrollment.Class.ClassId == selected.ClassId)
            .OrderBy(g => g.GradeType.Name)
            .ToList();

        var detailWindow = new ClassGradeDetailWindow(selected.ClassName, gradesForClass)
        {
            Owner = this
        };
        detailWindow.ShowDialog();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadGrades();
    }

    // Shared weighted-average calculation, reused by both the class-list summary
    // and ClassGradeDetailWindow so the two never disagree on the numbers.
    // Guards MaxScore > 0 (bug fixed earlier) AND totalWeight == 0 (no valid grade yet).
    public static string ComputeWeightedAverageDisplay(List<Grade> grades)
    {
        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var grade in grades)
        {
            if (grade.MaxScore > 0)
            {
                var normalizedScore = (grade.Score / grade.MaxScore) * 10;
                var weight = grade.GradeType.WeightPercent;
                totalWeightedScore += normalizedScore * weight;
                totalWeight += weight;
            }
        }

        if (totalWeight == 0) return "N/A";

        var weightedScore = Math.Round(totalWeightedScore / totalWeight, 2);
        return totalWeight < 100
            ? $"{weightedScore} (chưa đủ đầu điểm)"
            : weightedScore.ToString("F2");
    }
}
