using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  TopStudentsWindow — who is doing well on a course.
//  CONTENTS:
//    1. Construction  — fill the semester and course pickers
//    2. Show          — ask the service for the ranking
//    3. Filter/paging — the threshold checkbox, then the pager
//
//  A read-only report. It replaces the old Scholarship Review, minus the part
//  that granted tuition vouchers: one click there could mint a discount per
//  student, and those then had to be tracked, expired and reconciled against
//  invoices. Listing who qualifies is the useful half, and it is safe to press
//  as many times as you like.
// ============================================================
public partial class TopStudentsWindow : Window
{
    private readonly IStudentRankingService _rankingService = new StudentRankingService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ICourseService _courseService = new CourseService();

    private List<StudentRanking> _ranking = new();

    public TopStudentsWindow()
    {
        InitializeComponent();

        cboSemester.ItemsSource = _semesterService.GetAll()
            .OrderByDescending(s => s.StartDate)
            .ToList();
        cboCourse.ItemsSource = _courseService.GetAll()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToList();

        // Default to the semester running today — the one you almost always want.
        var today = DateOnly.FromDateTime(DateTime.Today);
        cboSemester.SelectedItem = _semesterService.GetAll()
            .FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate);
    }

    // ---- 2. Show -----------------------------------------------
    private void BtnShow_Click(object sender, RoutedEventArgs e)
    {
        if (cboSemester.SelectedValue is not int semesterId)
        {
            Warn("Please pick a semester.");
            return;
        }

        if (cboCourse.SelectedValue is not int courseId)
        {
            Warn("Please pick a course.");
            return;
        }

        if (!decimal.TryParse(txtThreshold.Text.Trim(), out var threshold) || threshold < 0 || threshold > 10)
        {
            Warn("The average has to be a number between 0 and 10.");
            return;
        }

        try
        {
            _ranking = _rankingService.GetRanking(semesterId, courseId, threshold);

            statTotal.Text = _ranking.Count.ToString();
            statAbove.Text = _ranking.Count(r => r.MeetsThreshold).ToString();
            statAboveLabel.Text = $"reach {threshold:0.#} or above";
            statPending.Text = _ranking.Count(r => !r.IsFullyMarked).ToString();
            summaryPanel.Visibility = Visibility.Visible;

            pager.Reset();
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not build the ranking:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 3. Filter & paging ------------------------------------
    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (dgRanking == null) return;   // fires once while the window is still loading
        pager.Reset();
        BindPage();
    }

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void BindPage()
    {
        // The service always returns everyone; hiding the rest is a view decision, so
        // un-ticking the box needs no second trip to the database.
        var shown = chkOnlyAbove.IsChecked == true
            ? _ranking.Where(r => r.MeetsThreshold).ToList()
            : _ranking;

        dgRanking.ItemsSource = pager.Slice(shown);

        emptyState.Visibility = shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (shown.Count == 0 && _ranking.Count > 0)
            emptyState.Text = "Nobody on this course reaches that average yet.";
        else if (shown.Count == 0)
            emptyState.Text = "No students are taking this course in that semester.";
    }

    private static void Warn(string message) =>
        MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
}
