using System.Linq;
using System.Windows;
using BusinessObjects;

namespace WpfApp;

public partial class ClassGradeDetailWindow : Window
{
    public ClassGradeDetailWindow(string className, List<Grade> grades)
    {
        InitializeComponent();
        Title = $"Grades — {className}";
        tbClassName.Text = className;

        var displayItems = grades
            .OrderBy(g => g.GradeType.Name)
            .Select(g => new GradeDetailDisplayItem
            {
                GradeTypeName = g.GradeType.Name,
                ScoreDisplay = $"{g.Score}/{g.MaxScore}",
                WeightPercent = $"{g.GradeType.WeightPercent}%",
                GradedAtDisplay = g.GradedAt.ToString("dd/MM/yyyy")
            })
            .ToList();

        dgGrades.ItemsSource = displayItems;

        // Reuse the exact same weighted-average calculation used in the
        // class-list screen (StudentGradeWindow), so the two never disagree.
        tbWeightedAverage.Text = $"Weighted Average: {StudentGradeWindow.ComputeWeightedAverageDisplay(grades)}";
    }
}