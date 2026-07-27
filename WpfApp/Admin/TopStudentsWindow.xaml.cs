using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  TopStudentsWindow — who is doing well this semester.
//  CONTENTS:
//    1. Construction — fill the semester picker, default to the current one
//    2. Show         — read the filters ONCE, ask the service, apply, display
//    3. Paging       — slice the already-filtered list
//
//  A read-only report. It replaces the old Scholarship Review, minus the part
//  that granted tuition vouchers: one click there could mint a discount per
//  student, and those then had to be tracked, expired and reconciled against
//  invoices. Listing who qualifies is the useful half, and it is safe to press
//  as many times as you like.
//
//  Nothing recalculates as you tick boxes — the grid only ever shows the result
//  of the last Show, so what is on screen always matches the filter bar you
//  pressed it with.
// ============================================================
public partial class TopStudentsWindow : Window
{
    private readonly IStudentRankingService _rankingService = new StudentRankingService();
    private readonly ISemesterService _semesterService = new SemesterService();

    /// <summary>The rows Show produced, already filtered. The pager slices this.</summary>
    private List<StudentRanking> _shown = new();

    public TopStudentsWindow()
    {
        InitializeComponent();

        var semesters = _semesterService.GetAll()
            .OrderByDescending(s => s.StartDate)
            .ToList();
        cboSemester.ItemsSource = semesters;

        // Default to the semester running today — the one you almost always want.
        var today = DateOnly.FromDateTime(DateTime.Today);
        cboSemester.SelectedItem = semesters.FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate)
                                   ?? semesters.FirstOrDefault();
    }

    // ---- 2. Show -----------------------------------------------
    private void BtnShow_Click(object sender, RoutedEventArgs e)
    {
        if (cboSemester.SelectedValue is not int semesterId)
        {
            Warn("Please pick a semester.");
            return;
        }

        if (!decimal.TryParse(txtThreshold.Text.Trim(), out var threshold) || threshold < 0 || threshold > 10)
        {
            Warn("The average has to be a number between 0 and 10.");
            return;
        }

        var onlyAbove = chkOnlyAbove.IsChecked == true;
        var onlyComplete = chkOnlyComplete.IsChecked == true;

        try
        {
            var all = _rankingService.GetRanking(semesterId, threshold);

            // Default on: a student still missing a component has an average over part
            // of the weights, which cannot be compared with a finished one. Untick the
            // box to see them anyway — the "40% marked" note says which is which.
            var filtered = all.AsEnumerable();
            if (onlyComplete) filtered = filtered.Where(r => r.IsFullyMarked);
            if (onlyAbove) filtered = filtered.Where(r => r.MeetsThreshold);

            _shown = filtered.ToList();

            statTotal.Text = all.Count.ToString();
            statAbove.Text = all.Count(r => r.MeetsThreshold && r.IsFullyMarked).ToString();
            statAboveLabel.Text = $"fully marked, {threshold:0.#}+";
            statPending.Text = all.Count(r => !r.IsFullyMarked).ToString();
            summaryPanel.Visibility = Visibility.Visible;

            SetEmptyMessage(all.Count, onlyAbove, onlyComplete, threshold);

            pager.Reset();
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not build the ranking:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Says which filter emptied the grid, rather than just showing nothing.</summary>
    private void SetEmptyMessage(int totalFound, bool onlyAbove, bool onlyComplete, decimal threshold)
    {
        if (totalFound == 0)
            emptyState.Text = "No students are enrolled in that semester.";
        else if (onlyAbove && onlyComplete)
            emptyState.Text = $"Nobody has finished all their marks with an average of {threshold:0.#} or above.\n"
                            + "Untick a box to widen the search.";
        else if (onlyComplete)
            emptyState.Text = "Nobody in that semester has all their marks in yet.";
        else if (onlyAbove)
            emptyState.Text = $"Nobody reaches an average of {threshold:0.#} yet.";
    }

    // ---- 3. Paging ---------------------------------------------
    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void BindPage()
    {
        dgRanking.ItemsSource = pager.Slice(_shown);
        emptyState.Visibility = _shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void Warn(string message) =>
        MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
}
