using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassRosterWindow : Window
{
    private readonly User _currentUser;
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private Teacher? _teacher;
    private List<Class> _teacherClasses = new();

    public ClassRosterWindow(User currentUser)
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
                .Where(c => c.TeacherId == _teacher.TeacherId)
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
            var enrollments = _enrollmentService.GetByClassId(cls.ClassId);
            if (!enrollments.Any())
            {
                MessageBox.Show($"No active enrollments for class '{cls.Name}'.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgRoster.ItemsSource = null;
                return;
            }

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

            dgRoster.ItemsSource = students;
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
        dgRoster.ItemsSource = null;
    }
}

public class RosterRow
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}