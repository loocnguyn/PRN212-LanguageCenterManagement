using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Microsoft.Win32;
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

    /// <summary>Just the number, for the pill in the grid.</summary>
    public string AverageText { get; set; } = "";

    /// <summary>False while only part of the weights have been marked — the pill greys out.</summary>
    public bool IsComplete { get; set; }
}

public class GradeDetailDisplayItem
{
    public string GradeTypeName { get; set; } = "";
    public string ScoreDisplay { get; set; } = "";
    public string WeightPercent { get; set; } = "";
    public string GradedAtDisplay { get; set; } = "";
}

// ============================================================
//  StudentGradeWindow — the student's grades, with Excel export.
//  CONTENTS:
//    1. Construction & LoadGrades — pull the student's grades
//    2. Semester/course filters   — cascading combos -> class list
//    3. Row actions & export      — class detail, .xlsx export
//    4. Helpers                   — weighted average
// ============================================================
public partial class StudentGradeWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IExcelExportService _excelExportService = new ExcelExportService();

    private int _studentId;
    private string _studentName = "";
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
            var student = _studentService.GetByUserId(_currentUser.Id);
            if (student == null)
            {
                tbStudentInfo.Text = "No student profile linked to this account.";
                ShowEmpty("No student profile is linked to this account.");
                return;
            }
            _studentId = student.StudentId;
            _studentName = student.FullName;
            tbStudentInfo.Text = student.FullName;

            _allGrades = _gradeService.GetByStudentId(_studentId);

            if (!_allGrades.Any())
            {
                ShowEmpty("No marks have been entered for you yet.\nThey appear here as your teachers record them.");
                return;
            }

            cbSemester.Visibility = Visibility.Visible;
            cbCourse.Visibility = Visibility.Visible;
            emptyState.Visibility = Visibility.Collapsed;

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
                emptyState.Text = "No marks match this filter.";
                emptyState.Visibility = Visibility.Visible;
                tbSummary.Text = "";
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
                    WeightedAverageDisplay = ComputeWeightedAverageDisplay(g.ToList()),
                    AverageText = ComputeAverageNumber(g.ToList()),
                    IsComplete = IsFullyMarked(g.ToList())
                })
                .ToList();

            dgClasses.ItemsSource = summaryItems;
            emptyState.Visibility = Visibility.Collapsed;

            var complete = summaryItems.Count(i => i.IsComplete);
            tbSummary.Text = $"{summaryItems.Count} class(es) · {complete} fully marked · "
                           + "double-click a class for the breakdown";
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
            .OrderBy(g => g.Component.Name)
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

    /// <summary>Exports the student's grade transcript (respecting the current Semester/Course
    /// filter) to an .xlsx file: one row per grade component, plus each class's weighted average.</summary>
    private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (!_allGrades.Any())
        {
            MessageBox.Show("No grades to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var gradesToExport = _allGrades.AsEnumerable();

        if (cbSemester.SelectedItem is SemesterFilterItem selectedSemester)
            gradesToExport = gradesToExport.Where(g => g.Enrollment.Class.SemesterId == selectedSemester.SemesterId);

        if (cbCourse.SelectedItem is CourseFilterItem selectedCourse && selectedCourse.CourseId != 0)
            gradesToExport = gradesToExport.Where(g => g.Enrollment.Class.CourseId == selectedCourse.CourseId);

        var gradeList = gradesToExport.ToList();
        if (!gradeList.Any())
        {
            MessageBox.Show("No grades match the current filter to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var safeName = string.Join("_", _studentName.Split(Path.GetInvalidFileNameChars()));

        var dialog = new SaveFileDialog
        {
            Title = "Lưu bảng điểm",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"BangDiem_{safeName}_{DateTime.Now:yyyyMMdd}.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var headers = new[]
            {
                "Semester", "Course", "Class", "GradeType", "Weight (%)",
                "Score", "MaxScore", "GradedAt", "ClassWeightedAverage"
            };

            var classGroups = gradeList
                .GroupBy(g => g.Enrollment.Class)
                .OrderByDescending(g => g.Key.Semester.StartDate)
                .ThenBy(g => g.Key.Name);

            var rows = new List<object?[]>();
            foreach (var classGroup in classGroups)
            {
                var classGrades = classGroup.OrderBy(g => g.Component.Name).ToList();
                var weightedAverage = ComputeWeightedAverageDisplay(classGrades);
                var cls = classGroup.Key;

                // The class average repeats on every row of the class so the sheet
                // stays flat — one row per component, ready to pivot.
                rows.AddRange(classGrades.Select(grade => new object?[]
                {
                    cls.Semester?.Name ?? "N/A",
                    cls.Course?.Name ?? "N/A",
                    cls.Name,
                    grade.Component.Name,
                    grade.Component.WeightPercent,
                    grade.Score,
                    grade.MaxScore,
                    grade.GradedAt.ToString("dd/MM/yyyy"),
                    weightedAverage
                }));
            }

            _excelExportService.ExportToExcel(dialog.FileName, "Bang diem", headers, rows);

            MessageBox.Show($"Exported {gradeList.Count} grade record(s) to:\n{dialog.FileName}", "Export Successful",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Clears the grid and explains why, instead of leaving a blank panel.</summary>
    private void ShowEmpty(string message)
    {
        cbSemester.Visibility = Visibility.Collapsed;
        cbCourse.Visibility = Visibility.Collapsed;
        dgClasses.ItemsSource = null;
        tbSummary.Text = "";
        emptyState.Text = message;
        emptyState.Visibility = Visibility.Visible;
    }

    /// <summary>The average on its own, without the "incomplete" wording — the pill says that.</summary>
    private static string ComputeAverageNumber(List<Grade> grades)
    {
        var display = ComputeWeightedAverageDisplay(grades);
        var space = display.IndexOf(' ');
        return space < 0 ? display : display[..space];
    }

    /// <summary>True once every weight in the class has a mark against it.</summary>
    private static bool IsFullyMarked(List<Grade> grades)
        => grades.Where(g => g.MaxScore > 0).Sum(g => g.Component.WeightPercent) >= 100;

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
                var weight = grade.Component.WeightPercent;
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
