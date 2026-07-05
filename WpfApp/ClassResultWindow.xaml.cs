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
    private readonly User _currentUser;
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();

    public ClassResultWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
    }

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
            var cls = _classService.GetById(classId);
            if (cls == null)
            {
                MessageBox.Show($"Class {classId} not found.", "Not Found",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // AUTHORIZATION: verify the logged-in teacher owns this class
            if (!AuthorizationHelper.AuthorizeTeacherForClass(_currentUser, _teacherService, cls, "view results"))
                return;

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

            // BATCH-LOAD grades for all enrollments in one query (fixes N+1)
            var enrollmentIds = enrollments.Select(e => e.EnrollmentId).ToList();
            var allGrades = _gradeService.GetByEnrollmentIds(enrollmentIds);
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
                    if (grade != null && grade.MaxScore > 0)
                    {
                        // Show the raw score in the column
                        dict[gt.Name] = grade.Score;
                        // Normalized contribution: (Score / MaxScore) * WeightPercent / 100
                        finalScore += (grade.Score / grade.MaxScore) * (gt.WeightPercent / 100m);
                    }
                    else
                    {
                        dict[gt.Name] = null;
                    }
                }

                // FinalScore as a decimal (0.00 - 1.00). Multiply by 100 for percentage display.
                row.FinalScore = Math.Round(finalScore, 4);
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
                    Header = $"{gt.Name}\n({gt.WeightPercent}%)",
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
                Header = "Final Score\n(w/weight)",
                Binding = new Binding("FinalScore")
                {
                    StringFormat = "P2"
                },
                Width = 110
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