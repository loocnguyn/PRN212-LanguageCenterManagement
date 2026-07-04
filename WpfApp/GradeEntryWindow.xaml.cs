using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class GradeEntryWindow : Window
{
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();
    private readonly IClassService _classService = new ClassService();

    private List<GradeType> _gradeTypes = new();
    private List<Enrollment> _enrollments = new();

    public GradeEntryWindow()
    {
        InitializeComponent();
        _gradeTypes = _gradeTypeService.GetAll();
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

            _enrollments = _enrollmentService.GetByClassId(classId);
            if (!_enrollments.Any())
            {
                MessageBox.Show($"No active enrollments for class '{cls.Name}'.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgGrades.ItemsSource = null;
                return;
            }

            if (!_gradeTypes.Any())
            {
                MessageBox.Show("No grade types defined. Please add grade types first.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgGrades.ItemsSource = null;
                return;
            }

            // Build flat rows: one per enrollment × grade type
            var rows = new List<GradeEntryRow>();
            foreach (var enrollment in _enrollments)
            {
                var existingGrades = _gradeService.GetByEnrollmentId(enrollment.EnrollmentId);
                foreach (var gt in _gradeTypes)
                {
                    var existing = existingGrades.FirstOrDefault(g => g.GradeTypeId == gt.GradeTypeId);
                    rows.Add(new GradeEntryRow
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        StudentId = enrollment.StudentId,
                        StudentName = enrollment.Student?.FullName ?? "",
                        GradeTypeId = gt.GradeTypeId,
                        GradeType = gt.Name,
                        MaxScore = gt.WeightPercent > 0 ? 10m : 0m,
                        Score = existing?.Score ?? 0m,
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
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Score.ToString()) || row.Score < 0)
                    continue;

                var grade = new Grade
                {
                    EnrollmentId = row.EnrollmentId,
                    GradeTypeId = row.GradeTypeId,
                    Score = row.Score,
                    MaxScore = row.MaxScore > 0 ? row.MaxScore : 10m,
                    Note = row.Note,
                    GradedAt = DateTime.Now
                };
                _gradeService.Upsert(grade);
                savedCount++;
            }

            MessageBox.Show($"Saved grades for {savedCount} row(s).", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
    public decimal MaxScore { get; set; }
    public decimal Score { get; set; }
    public string Note { get; set; } = "";
}
