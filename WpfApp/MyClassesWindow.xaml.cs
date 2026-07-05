using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public class ClassDisplayItem
{
    public string SemesterName { get; set; } = "";
    public string CourseDisplay { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string ClassroomName { get; set; } = "";
    public string DateRangeDisplay { get; set; } = "";
    public string ClassStatus { get; set; } = "";
    public string EnrollmentStatus { get; set; } = "";
}

public partial class MyClassesWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();

    private int _studentId;
    private List<Enrollment> _allEnrollments = new();

    public MyClassesWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadClasses();
    }

    private void LoadClasses()
    {
        try
        {
            var student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null)
            {
                tbStudentInfo.Text = "No student profile linked to this account.";
                dgClasses.ItemsSource = null;
                tbNoClasses.Visibility = Visibility.Collapsed;
                return;
            }
            _studentId = student.StudentId;
            tbStudentInfo.Text = $"Student: {student.FullName}";

            // Load all enrollments for this student
            _allEnrollments = _enrollmentService.GetByStudentId(_studentId);

            RefreshDisplay();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading classes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CbShowAll_Changed(object sender, RoutedEventArgs e)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        try
        {
            var displayEnrollments = _allEnrollments;

            // Filter based on checkbox
            if (!cbShowAll.IsChecked.GetValueOrDefault(false))
            {
                displayEnrollments = displayEnrollments.Where(en => en.Status == "ACTIVE").ToList();
            }

            if (!displayEnrollments.Any())
            {
                dgClasses.ItemsSource = null;
                tbNoClasses.Visibility = Visibility.Visible;
                tbSummary.Text = "";
                return;
            }

            // Create display items
            var displayItems = displayEnrollments
                .Select(en => new ClassDisplayItem
                {
                    SemesterName = en.Class?.Semester?.Name ?? "N/A",
                    CourseDisplay = $"{en.Class?.Course?.Name ?? "N/A"} ({en.Class?.Course?.Code ?? "N/A"})",
                    ClassName = en.Class?.Name ?? "N/A",
                    TeacherName = en.Class?.Teacher?.FullName ?? "N/A",
                    ClassroomName = en.Class?.Classroom?.Name ?? "N/A",
                    DateRangeDisplay = FormatDateRange(en.Class?.StartDate, en.Class?.EndDate),
                    ClassStatus = en.Class?.Status ?? "N/A",
                    EnrollmentStatus = en.Status ?? "N/A"
                })
                .ToList();

            // Sort: Semester descending by StartDate, then Class name ascending
            displayItems = displayItems
                .OrderByDescending(item => _allEnrollments
                    .First(en => en.Class?.Name == item.ClassName)
                    .Class?.Semester?.StartDate)
                .ThenBy(item => item.ClassName)
                .ToList();

            dgClasses.ItemsSource = displayItems;
            tbNoClasses.Visibility = Visibility.Collapsed;
            tbSummary.Text = $"Showing {displayItems.Count} class(es)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string FormatDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate == null || endDate == null)
            return "TBD";

        return $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
    }
}
