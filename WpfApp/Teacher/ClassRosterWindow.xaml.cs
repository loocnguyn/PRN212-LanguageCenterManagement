using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ClassRosterWindow — the student roster of a teacher's class.
//  CONTENTS:
//    1. Construction & LoadTeacherData — the teacher's classes
//    2. Cascading selects              — semester->course->class->roster
//    3. Reset                          — clear the view
// ============================================================
public partial class ClassRosterWindow : Window
{
    private readonly User _currentUser;
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private Teacher? _teacher;
    private List<Class> _teacherClassesInSemester = new();
    private List<RosterRow> _roster = new();

    public ClassRosterWindow(User currentUser)
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
        SetRoster(new List<RosterRow>());
        _teacherClassesInSemester = new List<Class>();

        if (cboSemester.SelectedItem is not Semester semester || _teacher == null) return;

        try
        {
            _teacherClassesInSemester = _classService.GetClassesForTeacher(_teacher.TeacherId, semester.SemesterId);

            var courses = _teacherClassesInSemester
                .Select(c => c.Course)
                .Where(c => c != null)
                .DistinctBy(c => c.CourseId)
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
        SetRoster(new List<RosterRow>());

        if (cboCourse.SelectedItem is not Course course) return;

        var classesForCourse = _teacherClassesInSemester
            .Where(c => c.CourseId == course.CourseId)
            .ToList();

        cboClass.ItemsSource = classesForCourse;
    }

    /// <summary>Step 4: Class selected -> load the roster grid.</summary>
    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls) { classCard.Visibility = Visibility.Collapsed; return; }

        try
        {
            // Class summary card — what is on screen, and how full the class is.
            classCard.Visibility = Visibility.Visible;
            tbClassName.Text = cls.Name;
            tbClassDetail.Text = $"{cls.SnapCourseCode} — {cls.SnapCourseName} · "
                               + $"{cls.StartDate:dd/MM/yyyy} – {cls.EndDate:dd/MM/yyyy} · {cls.Status}";

            var enrollments = _enrollmentService.GetByClassId(cls.ClassId);

            var students = enrollments
                .Where(en => en.Student != null)
                .Select(en => new RosterRow
                {
                    StudentId = en.Student!.StudentId,
                    FullName = en.Student.FullName,
                    Gender = en.Student.Gender ?? "",
                    Phone = en.Student.Phone ?? "",
                    Email = en.Student.Email ?? ""
                })
                .ToList();

            tbCount.Text = $"{students.Count} / {cls.MaxStudents} enrolled";
            SetRoster(students);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading roster: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        cboClass.SelectedIndex = -1;
        classCard.Visibility = Visibility.Collapsed;
        SetRoster(new List<RosterRow>());
    }

    /// <summary>Swap the roster the grid is paging over and jump back to page 1.</summary>
    private void SetRoster(List<RosterRow> roster)
    {
        _roster = roster;
        pager.Reset();
        BindPage();
    }

    private void BindPage()
    {
        dgRoster.ItemsSource = pager.Slice(_roster);

        emptyState.Visibility = _roster.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        emptyState.Text = cboClass.SelectedItem == null
            ? "Pick a class to see its students."
            : "Nobody is enrolled in this class yet.";
    }

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();
}

public class RosterRow
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}
