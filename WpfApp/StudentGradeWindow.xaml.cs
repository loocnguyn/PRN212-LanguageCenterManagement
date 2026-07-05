using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public class GradeDisplayItem
{
    public string SemesterName { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string GradeTypeName { get; set; } = "";
    public string ScoreDisplay { get; set; } = "";
    public string WeightPercent { get; set; } = "";
    public string GradedAtDisplay { get; set; } = "";
}

public partial class StudentGradeWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IGradeService _gradeService = new GradeService();

    private int _studentId;

    public StudentGradeWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadGrades();
    }

    private void LoadGrades()
    {
        try
        {
            var student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null)
            {
                tbStudentInfo.Text = "No student profile linked to this account.";
                tbSummary.Text = "";
                dgGrades.ItemsSource = null;
                return;
            }
            _studentId = student.StudentId;
            tbStudentInfo.Text = $"Student: {student.FullName}";

            var grades = _gradeService.GetByStudentId(_studentId);

            if (!grades.Any())
            {
                tbSummary.Text = "You have no grades yet.";
                dgGrades.ItemsSource = null;
                return;
            }

            var displayItems = new List<GradeDisplayItem>();

            // Group by Semester (descending by StartDate), then Class, then GradeType
            var groupedBySemester = grades
                .GroupBy(g => g.Enrollment.Class.Semester)
                .OrderByDescending(sg => sg.Key.StartDate)
                .ToList();

            foreach (var semesterGroup in groupedBySemester)
            {
                var semester = semesterGroup.Key;

                var groupedByClass = semesterGroup
                    .GroupBy(g => g.Enrollment.Class)
                    .OrderBy(cg => cg.Key.Name)
                    .ToList();

                foreach (var classGroup in groupedByClass)
                {
                    var cls = classGroup.Key;
                    var course = cls.Course;

                    // Sort grades by GradeType name within each class
                    var sortedGrades = classGroup.OrderBy(g => g.GradeType.Name).ToList();

                    // Calculate weighted score for this class
                    decimal totalWeightedScore = 0;
                    decimal totalWeight = 0;

                    foreach (var grade in sortedGrades)
                    {
                        var normalizedScore = (grade.Score / grade.MaxScore) * 10; // Normalize to 10
                        var weight = grade.GradeType.WeightPercent;
                        totalWeightedScore += normalizedScore * weight;
                        totalWeight += weight;

                        var displayItem = new GradeDisplayItem
                        {
                            SemesterName = semester.Name,
                            CourseName = course?.Name ?? "N/A",
                            ClassName = cls.Name,
                            GradeTypeName = grade.GradeType.Name,
                            ScoreDisplay = $"{grade.Score}/{grade.MaxScore}",
                            WeightPercent = $"{grade.GradeType.WeightPercent}%",
                            GradedAtDisplay = grade.GradedAt.ToString("dd/MM/yyyy")
                        };
                        displayItems.Add(displayItem);
                    }

                    // Add summary row for this class
                    string weightedScoreText;
                    if (totalWeight < 100)
                    {
                        var weightedScore = Math.Round(totalWeightedScore / totalWeight, 2);
                        weightedScoreText = $"{weightedScore} (chưa đủ đầu điểm)";
                    }
                    else
                    {
                        var weightedScore = Math.Round(totalWeightedScore / totalWeight, 2);
                        weightedScoreText = weightedScore.ToString("F2");
                    }

                    var summaryItem = new GradeDisplayItem
                    {
                        SemesterName = "",
                        CourseName = "",
                        ClassName = $"[CLASS TOTAL: {cls.Name}]",
                        GradeTypeName = "",
                        ScoreDisplay = weightedScoreText,
                        WeightPercent = $"{totalWeight}%",
                        GradedAtDisplay = ""
                    };
                    displayItems.Add(summaryItem);
                }
            }

            dgGrades.ItemsSource = displayItems;
            tbSummary.Text = $"Showing {grades.Count} grade record(s)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading grades: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadGrades();
    }
}

