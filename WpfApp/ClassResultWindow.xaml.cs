using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassResultWindow : Window
{
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();

    public ClassResultWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtClassId.Text.Trim(), out var classId))
        {
            MessageBox.Show("Please enter a valid numeric Class ID.", "Invalid Input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var gradeTypes = _gradeTypeService.GetAll();
            if (gradeTypes.Count == 0)
            {
                MessageBox.Show("No grade types configured.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enrollments = _enrollmentService.GetByClassId(classId);
            if (enrollments.Count == 0)
            {
                MessageBox.Show($"No active enrollments found for Class ID {classId}.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgResults.ItemsSource = null;
                return;
            }

            // Enrich: eagerly load grades for all enrollments
            var enrollmentIds = enrollments.Select(e => e.EnrollmentId).ToList();
            var allGrades = new List<Grade>();
            foreach (var eid in enrollmentIds)
            {
                allGrades.AddRange(_gradeService.GetByEnrollmentId(eid));
            }
            var gradesByEnrollment = allGrades
                .GroupBy(g => g.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build rows
            var rows = new List<ExpandoObject>();
            foreach (var enrollment in enrollments)
            {
                dynamic row = new ExpandoObject();
                var dict = (IDictionary<string, object?>)row;

                row.StudentId = enrollment.Student?.StudentId ?? 0;
                row.StudentName = enrollment.Student?.FullName ?? "";

                var enrollmentGrades = gradesByEnrollment.TryGetValue(enrollment.EnrollmentId, out var gList)
                    ? gList
                    : new List<Grade>();

                decimal finalScore = 0m;
                foreach (var gt in gradeTypes)
                {
                    var grade = enrollmentGrades.FirstOrDefault(g => g.GradeTypeId == gt.GradeTypeId);
                    if (grade != null)
                    {
                        dict[gt.Name] = grade.Score;
                        finalScore += grade.Score * (gt.WeightPercent / 100m);
                    }
                    else
                    {
                        dict[gt.Name] = null;
                    }
                }

                row.FinalScore = Math.Round(finalScore, 2);
                rows.Add(row);
            }

            // Build dynamic columns
            dgResults.Columns.Clear();

            dgResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Student ID",
                Binding = new Binding("StudentId"),
                Width = 100
            });

            dgResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Full Name",
                Binding = new Binding("StudentName"),
                Width = 180
            });

            foreach (var gt in gradeTypes)
            {
                dgResults.Columns.Add(new DataGridTextColumn
                {
                    Header = $"{gt.Name} ({gt.WeightPercent}%)",
                    Binding = new Binding($"[{gt.Name}]")
                    {
                        TargetNullValue = "-",
                        StringFormat = "N2"
                    },
                    Width = 100
                });
            }

            dgResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Final Score",
                Binding = new Binding("FinalScore")
                {
                    StringFormat = "N2"
                },
                Width = 100
            });

            dgResults.ItemsSource = rows;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading results: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtClassId.Text = "";
        dgResults.Columns.Clear();
        dgResults.ItemsSource = null;
    }
}
