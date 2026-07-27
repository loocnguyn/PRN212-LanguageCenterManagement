using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  GradeEntryWindow — teacher enters/edits grades for a class.
//  CONTENTS:
//    1. Construction & LoadTeacherData — the teacher's classes
//    2. Cascading selects              — semester->course->class grid
//    3. LoadGradesTable                — dynamically build columns & DataTable rows
//    4. Save / Reset                   — persist grades; clear
//    5. Dynamic Calculations           — ColumnChanged handler & weighted average formula
// ============================================================
public partial class GradeEntryWindow : Window
{
    private readonly User _currentUser;
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private List<ClassGradeComponent> _components = new();
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

            tbTeacherInfo.Text = _teacher.FullName;

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
        dgGrades.Columns.Clear();
        dgGrades.ItemsSource = null;
        _teacherClassesInSemester = new List<Class>();

        if (cboSemester.SelectedItem is not Semester semester || _teacher == null) return;

        try
        {
            _teacherClassesInSemester = _classService.GetClassesWithDetails(semester.SemesterId)
                .Where(c => c.ClassTeachers.Any(ct => ct.TeacherId == _teacher.TeacherId))
                .ToList();

            var courses = _teacherClassesInSemester
                .Where(c => c.Course != null)
                .Select(c => c.Course)
                .GroupBy(c => c.CourseId)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();

            cboCourse.ItemsSource = courses;

            tbTeacherInfo.Text = courses.Any()
                ? _teacher.FullName
                : $"{_teacher.FullName} — no classes in {semester.Name}";
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
        dgGrades.Columns.Clear();
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
        dgGrades.Columns.Clear();
        dgGrades.ItemsSource = null;

        if (cboClass.SelectedItem is not Class cls) { ShowEmpty("Pick a class to start entering grades."); return; }

        try
        {
            // AUTHORIZATION: verify the logged-in teacher owns this class
            if (!AuthorizationHelper.AuthorizeTeacherForClass(_currentUser, _teacherService, cls, "access grades"))
                return;

            LoadGradesTable(cls);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading grade data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Step 5: Load/refresh the dynamic DataTable and bind it to dgGrades.</summary>
    private void LoadGradesTable(Class cls)
    {
        // The class's OWN frozen structure, captured when it was created. The
        // course template may have changed since; these weights must not.
        _components = _classService.GetGradeComponents(cls.ClassId);
        if (!_components.Any())
        {
            ShowEmpty($"'{cls.Name}' has no grading structure, so there is nothing to mark.");
            return;
        }

        _enrollments = _enrollmentService.GetByClassId(cls.ClassId);
        if (!_enrollments.Any())
        {
            ShowEmpty($"Nobody is enrolled in '{cls.Name}' yet.");
            return;
        }

        // BATCH-LOAD all grades for all enrollments in one query
        var enrollmentIds = _enrollments.Select(e => e.EnrollmentId).ToList();
        var allGrades = _gradeService.GetByEnrollmentIds(enrollmentIds);
        var gradesByEnrollment = allGrades
            .GroupBy(g => g.EnrollmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build dynamic columns for DataGrid
        dgGrades.Columns.Clear();

        // 1. Static Student ID column (Read-Only)
        dgGrades.Columns.Add(new DataGridTextColumn
        {
            Header = "Student ID",
            Binding = new Binding("StudentId"),
            IsReadOnly = true,
            Width = DataGridLength.Auto
        });

        // 2. Static Student Name column (Read-Only)
        dgGrades.Columns.Add(new DataGridTextColumn
        {
            Header = "Student Name",
            Binding = new Binding("StudentName"),
            IsReadOnly = true,
            Width = new DataGridLength(1.5, DataGridLengthUnitType.Star)
        });

        // 3. Dynamic columns for each grade component
        foreach (var comp in _components)
        {
            dgGrades.Columns.Add(new DataGridTextColumn
            {
                Header = $"{comp.Name}\n({comp.WeightPercent}%) Score",
                Binding = new Binding($"Score_{comp.ComponentId}")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                },
                Width = DataGridLength.Auto
            });

            dgGrades.Columns.Add(new DataGridTextColumn
            {
                Header = $"{comp.Name}\nNote",
                Binding = new Binding($"Note_{comp.ComponentId}")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        // 4. Static Total Score column (Read-Only, Bold)
        dgGrades.Columns.Add(new DataGridTextColumn
        {
            Header = "Total Score",
            Binding = new Binding("TotalScore"),
            IsReadOnly = true,
            Width = DataGridLength.Auto,
            FontWeight = FontWeights.Bold
        });

        // Create DataTable backing structure
        var dt = new DataTable();
        dt.Columns.Add("EnrollmentId", typeof(int));
        dt.Columns.Add("StudentId", typeof(int));
        dt.Columns.Add("StudentName", typeof(string));

        foreach (var comp in _components)
        {
            dt.Columns.Add($"Score_{comp.ComponentId}", typeof(decimal));
            dt.Columns.Add($"Note_{comp.ComponentId}", typeof(string));
        }

        dt.Columns.Add("TotalScore", typeof(string));

        // Populate table rows
        foreach (var enrollment in _enrollments)
        {
            var row = dt.NewRow();
            row["EnrollmentId"] = enrollment.EnrollmentId;
            row["StudentId"] = enrollment.StudentId;
            row["StudentName"] = enrollment.Student?.FullName ?? "";

            var enrollmentGrades = gradesByEnrollment.TryGetValue(enrollment.EnrollmentId, out var gList)
                ? gList
                : new List<Grade>();

            foreach (var comp in _components)
            {
                var existing = enrollmentGrades.FirstOrDefault(g => g.ComponentId == comp.ComponentId);
                if (existing != null)
                {
                    row[$"Score_{comp.ComponentId}"] = existing.Score;
                    row[$"Note_{comp.ComponentId}"] = existing.Note ?? "";
                }
                else
                {
                    row[$"Score_{comp.ComponentId}"] = DBNull.Value;
                    row[$"Note_{comp.ComponentId}"] = "";
                }
            }

            // Compute initial row total score
            CalculateRowTotalScore(row);

            dt.Rows.Add(row);
        }

        // Hook up the dynamic cell-recalculation event
        dt.ColumnChanged += DataTable_ColumnChanged;

        dgGrades.ItemsSource = dt.DefaultView;

        // Summary card: which class, what the marks are weighted by, how many students.
        classCard.Visibility = Visibility.Visible;
        tbClassName.Text = cls.Name;
        tbWeights.Text = string.Join(" · ", _components.Select(c => $"{c.Name} {c.WeightPercent}%"));
        tbCount.Text = $"{_enrollments.Count} student(s)";
        emptyState.Visibility = Visibility.Collapsed;
    }

    /// <summary>Clears the grid and says why it is empty, instead of a popup to dismiss.</summary>
    private void ShowEmpty(string message)
    {
        dgGrades.Columns.Clear();
        dgGrades.ItemsSource = null;
        classCard.Visibility = Visibility.Collapsed;
        emptyState.Text = message;
        emptyState.Visibility = Visibility.Visible;
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        cboSemester.SelectedIndex = -1;
        cboCourse.ItemsSource = null;
        cboClass.ItemsSource = null;
        _enrollments.Clear();
        ShowEmpty("Pick a class to start entering grades.");
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var view = dgGrades.ItemsSource as DataView;
        if (view == null)
        {
            MessageBox.Show("No grade data to save. Select a class first.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dt = view.Table;
        if (dt == null) return;
        try
        {
            var savedCount = 0;
            var errors = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                var enrollmentId = Convert.ToInt32(row["EnrollmentId"]);
                var studentName = row["StudentName"].ToString() ?? "";

                foreach (var comp in _components)
                {
                    var scoreObj = row[$"Score_{comp.ComponentId}"];
                    var noteObj = row[$"Note_{comp.ComponentId}"];

                    if (scoreObj == DBNull.Value || scoreObj == null || string.IsNullOrWhiteSpace(scoreObj.ToString()))
                    {
                        // If score is omitted or cleared, do not attempt to upsert it
                        continue;
                    }

                    if (!decimal.TryParse(scoreObj.ToString(), out decimal score))
                    {
                        errors.Add($"{studentName} - {comp.Name}: Score must be a valid number.");
                        continue;
                    }

                    var maxScore = 10m; // standard default max score
                    if (score < 0 || score > maxScore)
                    {
                        errors.Add($"{studentName} - {comp.Name}: Score {score} is out of range (0-{maxScore}).");
                        continue;
                    }

                    var note = noteObj?.ToString() ?? "";

                    var grade = new Grade
                    {
                        EnrollmentId = enrollmentId,
                        ComponentId = comp.ComponentId,
                        Score = score,
                        MaxScore = maxScore,
                        Note = note,
                        GradedAt = DateTime.Now
                    };
                    _gradeService.Upsert(grade);
                    savedCount++;
                }
            }

            var msg = $"Saved grades for {savedCount} score(s).";
            if (errors.Any())
                msg += $"\n\nValidation errors ({errors.Count}):\n" + string.Join("\n", errors.Take(10));

            MessageBox.Show(msg, "Result",
                MessageBoxButton.OK, errors.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);

            // Reload grades table to refresh data state
            if (cboClass.SelectedItem is Class cls)
            {
                LoadGradesTable(cls);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving grades: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 5. Dynamic Calculations ---------------------------------

    private void DataTable_ColumnChanged(object sender, DataColumnChangeEventArgs e)
    {
        if (e.Column?.ColumnName != null && e.Column.ColumnName.StartsWith("Score_"))
        {
            if (sender is DataTable dt)
            {
                // Unsubscribe temporarily to avoid firing recursive ColumnChanged notifications
                dt.ColumnChanged -= DataTable_ColumnChanged;
                CalculateRowTotalScore(e.Row);
                dt.ColumnChanged += DataTable_ColumnChanged;
            }
        }
    }

    private void CalculateRowTotalScore(DataRow row)
    {
        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var comp in _components)
        {
            var scoreVal = row[$"Score_{comp.ComponentId}"];
            if (scoreVal != DBNull.Value && scoreVal != null && !string.IsNullOrWhiteSpace(scoreVal.ToString()))
            {
                if (decimal.TryParse(scoreVal.ToString(), out decimal score))
                {
                    decimal maxScore = 10m; // Default max score is 10
                    decimal normalizedScore = (score / maxScore) * 10m;
                    decimal weight = comp.WeightPercent;
                    totalWeightedScore += normalizedScore * weight;
                    totalWeight += weight;
                }
            }
        }

        if (totalWeight == 0)
        {
            row["TotalScore"] = "N/A";
        }
        else
        {
            decimal weightedScore = Math.Round(totalWeightedScore / totalWeight, 2);
            if (totalWeight < 100)
            {
                row["TotalScore"] = $"{weightedScore} (chưa đủ đầu điểm)";
            }
            else
            {
                row["TotalScore"] = weightedScore.ToString("F2");
            }
        }
    }
}
