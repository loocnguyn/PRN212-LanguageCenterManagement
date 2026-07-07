using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class GradeEntryWindow : Window
{
    private readonly User _currentUser;
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();

    private List<GradeType> _gradeTypes = new();
    private List<Enrollment> _enrollments = new();

    public GradeEntryWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
    }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(txtClassId.Text.Trim(), out var classId))
            {
                MessageBox.Show("Please enter a valid Class ID.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cls = _classService.GetById(classId);
            if (cls == null)
            {
                MessageBox.Show($"Class {classId} not found.", "Not Found",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // AUTHORIZATION: verify the logged-in teacher owns this class
            if (!AuthorizationHelper.AuthorizeTeacherForClass(_currentUser, _teacherService, cls, "access grades"))
                return;

            // Load grade types on demand (not in constructor — avoids DB call at init)
            _gradeTypes = _gradeTypeService.GetAll();
            if (!_gradeTypes.Any())
            {
                MessageBox.Show("No grade types defined. Please add grade types first.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgGrades.ItemsSource = null;
                return;
            }

            _enrollments = _enrollmentService.GetByClassId(classId);
            if (!_enrollments.Any())
            {
                MessageBox.Show($"No active enrollments for class '{cls.Name}'.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgGrades.ItemsSource = null;
                return;
            }

            // BATCH-LOAD all grades for all enrollments in one query (fixes N+1)
            var enrollmentIds = _enrollments.Select(e => e.EnrollmentId).ToList();
            var allGrades = _gradeService.GetByEnrollmentIds(enrollmentIds);
            var gradesByEnrollment = allGrades
                .GroupBy(g => g.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build flat rows: one per enrollment × grade type
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
        txtClassId.Text = "";
        dgGrades.ItemsSource = null;
        _enrollments.Clear();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (dgGrades.ItemsSource is not List<GradeEntryRow> rows || !rows.Any())
        {
            MessageBox.Show("No grade data to save. Load a class first.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var savedCount = 0;
            var errors = new List<string>();

            foreach (var row in rows)
            {
                // Skip rows with no score entered
                if (row.Score is null)
                    continue;

                // VALIDATION: score must be between 0 and MaxScore
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