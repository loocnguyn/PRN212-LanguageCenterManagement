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

// ============================================================
//  ClassResultWindow — final results/summary for a teacher's class.
//  CONTENTS:
//    1. Construction & LoadTeacherClasses — the teacher's classes
//    2. Class select                      — compute & show results
//    3. Reset                             — clear the view
// ============================================================
public partial class ClassResultWindow : Window
{
    private readonly User _currentUser;
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private Teacher? _teacher;
    private List<Class> _teacherClasses = new();
    private List<ExpandoObject> _rows = new();

    public ClassResultWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadTeacherClasses();
    }

    private void LoadTeacherClasses()
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

            var semester = _semesterService.GetActive();
            if (semester == null)
            {
                tbTeacherInfo.Text += " — No active semester";
                return;
            }

            _teacherClasses = _classService.GetBySemesterId(semester.SemesterId)
                .Where(c => c.ClassTeachers.Any(ct => ct.TeacherId == _teacher.TeacherId))
                .ToList();

            cboClass.ItemsSource = _teacherClasses;
            cboClass.SelectedIndex = -1;

            if (!_teacherClasses.Any())
                tbTeacherInfo.Text += $" — No classes in {semester.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading teacher classes: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls) return;

        try
        {
            // The class's OWN frozen structure — not the course template, which may
            // have been edited since this class opened.
            var components = _classService.GetGradeComponents(cls.ClassId);
            if (components.Count == 0)
            {
                MessageBox.Show($"No grading structure recorded for class '{cls.Name}'.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enrollments = _enrollmentService.GetByClassId(cls.ClassId);
            if (enrollments.Count == 0)
            {
                MessageBox.Show($"No active enrollments found for class '{cls.Name}'.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                SetRows(new List<ExpandoObject>());
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
                foreach (var comp in components)
                {
                    var grade = enrollmentGrades.FirstOrDefault(g => g.ComponentId == comp.ComponentId);
                    if (grade != null && grade.MaxScore > 0)
                    {
                        dict[comp.Name] = grade.Score;
                        finalScore += (grade.Score / grade.MaxScore) * (comp.WeightPercent / 100m);
                    }
                    else
                    {
                        dict[comp.Name] = null;
                    }
                }

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

            foreach (var comp in components)
            {
                dgResults.Columns.Add(new DataGridTextColumn
                {
                    Header = $"{comp.Name}\n({comp.WeightPercent}%)",
                    Binding = new Binding($"[{comp.Name}]")
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

            SetRows(rows);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading results: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        cboClass.SelectedIndex = -1;
        dgResults.Columns.Clear();
        SetRows(new List<ExpandoObject>());
    }

    /// <summary>Swap the result set the grid is paging over and jump back to page 1.</summary>
    private void SetRows(List<ExpandoObject> rows)
    {
        _rows = rows;
        pager.Reset();
        BindPage();
    }

    private void BindPage() => dgResults.ItemsSource = pager.Slice(_rows);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();
}