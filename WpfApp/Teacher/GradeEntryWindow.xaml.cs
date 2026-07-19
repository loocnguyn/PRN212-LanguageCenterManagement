using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  GradeEntryWindow — teacher enters/edits grades for a class.
//  CONTENTS:
//    1. Construction & LoadTeacherData — the teacher's classes
//    2. Cascading selects              — semester->course->class grid
//    3. Save / Reset                   — persist grades; clear
// ============================================================
public partial class GradeEntryWindow : Window
{
    private readonly User _currentUser;
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private List<GradeType> _gradeTypes = new();
    private List<Enrollment> _enrollments = new();
    private Teacher? _teacher;
    private List<Class> _teacherClassesInSemester = new();

    public GradeEntryWindow(User currentUser)
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
                tbTeacherInfo.Text = "No teacher profile linked to this account.";
                return;
            }

            tbTeacherInfo.Text = $"Teacher: {_teacher.FullName}";

            var semesters = _semesterService.GetAll()
                .OrderByDescending(s => s.StartDate)
                .ToList();

            cboSemester.ItemsSource = semesters;

            if (!semesters.Any())
            {
                tbTeacherInfo.Text += " — No semesters found";
                return;
            }

            var active = semesters.FirstOrDefault(s => s.IsActive) ?? semesters.First();
            cboSemester.SelectedItem = active;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading teacher data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 2: Semester selected -> populate the Course dropdown with this
    /// teacher's courses in that semester.</summary>
    private void CboSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cboCourse.ItemsSource = null;
        cboClass.ItemsSource = null;
        dgGrades.ItemsSource = null;
        _teacherClassesInSemester = new List<Class>();

        if (cboSemester.SelectedItem is not Semester semester || _teacher == null) return;

        try
        {
            _teacherClassesInSemester = _classService.GetClassesWithDetails(semester.SemesterId)
                .Where(c => c.TeacherId == _teacher.TeacherId)
                .ToList();

            var courses = _teacherClassesInSemester
                .Where(c => c.Course != null)
                .Select(c => c.Course)
                .GroupBy(c => c.CourseId)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();

            cboCourse.ItemsSource = courses;

            if (!courses.Any())
                tbTeacherInfo.Text = $"Teacher: {_teacher.FullName} — No classes in {semester.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading courses: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 3: Course selected -> populate the Class dropdown, filtered to
    /// classes of that course, in the chosen semester, taught by this teacher.</summary>
    private void CboCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cboClass.ItemsSource = null;
        dgGrades.ItemsSource = null;

        if (cboCourse.SelectedItem is not Course course) return;

        var classesForCourse = _teacherClassesInSemester
            .Where(c => c.CourseId == course.CourseId)
            .ToList();

        cboClass.ItemsSource = classesForCourse;
    }

    /// <summary>Step 4: Class selected -> load the grade entry grid.</summary>
    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls) return;

        try
        {
            // AUTHORIZATION: verify the logged-in teacher owns this class
            if (!AuthorizationHelper.AuthorizeTeacherForClass(_currentUser, _teacherService, cls, "access grades"))
                return;

            // Load the grading structure configured for this class's course
            _gradeTypes = _gradeTypeService.GetByCourseId(cls.CourseId);
            if (!_gradeTypes.Any())
            {
                MessageBox.Show($"No grade types configured for course '{cls.Name}'. Please set up its grading structure first.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgGrades.ItemsSource = null;
                return;
            }

            _enrollments = _enrollmentService.GetByClassId(cls.ClassId);
            if (!_enrollments.Any())
            {
                MessageBox.Show($"No active enrollments for class '{cls.Name}'.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgGrades.ItemsSource = null;
                return;
            }

            // BATCH-LOAD all grades for all enrollments in one query
            var enrollmentIds = _enrollments.Select(e => e.EnrollmentId).ToList();
            var allGrades = _gradeService.GetByEnrollmentIds(enrollmentIds);
            var gradesByEnrollment = allGrades
                .GroupBy(g => g.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build flat rows: one per enrollment x grade type
            var rows = new List<GradeEntryRow>();
            foreach (var enrollment in _enrollments)
            {
                var enrollmentGrades = gradesByEnrollment.TryGetValue(enrollment.EnrollmentId, out var gList)
                    ? gList
                    : new List<Grade>();

                foreach (var gt in _gradeTypes)
                {
                    var existing = enrollmentGrades.FirstOrDefault(g => g.GradeTypeId == gt.GradeTypeId);
                    rows.Add(new GradeEntryRow
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        StudentId = enrollment.StudentId,
                        StudentName = enrollment.Student?.FullName ?? "",
                        GradeTypeId = gt.GradeTypeId,
                        GradeType = gt.Name,
                        WeightPercent = gt.WeightPercent,
                        MaxScore = existing?.MaxScore > 0 ? existing.MaxScore : 10m,
                        Score = existing?.Score,
                        Note = existing?.Note ?? ""
                    });
                }
            }

            dgGrades.ItemsSource = rows;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading grade data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        cboClass.SelectedIndex = -1;
        dgGrades.ItemsSource = null;
        _enrollments.Clear();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (dgGrades.ItemsSource is not List<GradeEntryRow> rows || !rows.Any())
        {
            MessageBox.Show("No grade data to save. Select a class first.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var savedCount = 0;
            var errors = new List<string>();

            foreach (var row in rows)
            {
                if (row.Score is null)
                    continue;

                var maxScore = row.MaxScore > 0 ? row.MaxScore : 10m;
                if (row.Score < 0 || row.Score > maxScore)
                {
                    errors.Add($"{row.StudentName} - {row.GradeType}: Score {row.Score} is out of range (0-{maxScore}).");
                    continue;
                }

                var grade = new Grade
                {
                    EnrollmentId = row.EnrollmentId,
                    GradeTypeId = row.GradeTypeId,
                    Score = row.Score.Value,
                    MaxScore = maxScore,
                    Note = row.Note,
                    GradedAt = DateTime.Now
                };
                _gradeService.Upsert(grade);
                savedCount++;
            }

            var msg = $"Saved grades for {savedCount} row(s).";
            if (errors.Any())
                msg += $"\n\nValidation errors ({errors.Count}):\n" + string.Join("\n", errors.Take(10));

            MessageBox.Show(msg, "Result",
                MessageBoxButton.OK, errors.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving grades: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class GradeEntryRow
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public int GradeTypeId { get; set; }
    public string GradeType { get; set; } = "";
    public decimal WeightPercent { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? Score { get; set; }
    public string Note { get; set; } = "";
}
