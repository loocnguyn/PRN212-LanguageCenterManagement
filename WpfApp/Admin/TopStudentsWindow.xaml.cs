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
//  Two rules are built in rather than offered as options, because both answers
//  are the wrong one often enough to be a trap:
//    · only students who clear the threshold are listed — that IS the question;
//    · only students whose marks are ALL in are listed, since an average over
//      40% of the weights cannot be ranked against a finished one.
//  The counters above the grid still report the totals both rules excluded, so
//  nothing disappears silently.
// ============================================================
public partial class TopStudentsWindow : Window
{
    private readonly IStudentRankingService _rankingService = new StudentRankingService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly IAwardService _awardService = new AwardService();
    private readonly User _currentUser;

    /// <summary>The rows Show produced, already filtered. The pager slices this.</summary>
    private List<StudentRanking> _shown = new();

    /// <summary>The semester and pass mark the rows on screen were produced with —
    /// not whatever is in the boxes now. Awarding must use what was actually shown.</summary>
    private int _shownSemesterId;
    private decimal _shownThreshold;

    public TopStudentsWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;

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

        try
        {
            var all = _rankingService.GetRanking(semesterId, threshold);

            _shown = all.Where(r => r.IsFullyMarked && r.MeetsThreshold).ToList();

            statTotal.Text = all.Count.ToString();
            statAbove.Text = _shown.Count.ToString();
            statAboveLabel.Text = $"reach {threshold:0.#} or above";
            statPending.Text = all.Count(r => !r.IsFullyMarked).ToString();
            summaryPanel.Visibility = Visibility.Visible;

            SetEmptyMessage(all.Count, onlyAbove: true, onlyComplete: true, threshold);

            // Remember what these rows are FOR. If the user then edits the semester
            // or the mark without pressing Show, awarding still uses what is on screen.
            _shownSemesterId = semesterId;
            _shownThreshold = threshold;
            awardBar.Visibility = Visibility.Visible;

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
        // Slice returns the same row objects on every page, so a tick made on page 1
        // is still there after a trip to page 2 and back.
        dgRanking.ItemsSource = pager.Slice(_shown);
        emptyState.Visibility = _shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateTickedCount();
    }

    // ---- 4. Awarding -------------------------------------------
    private void BtnTickAll_Click(object sender, RoutedEventArgs e) => SetTicks(true);

    private void BtnClearTicks_Click(object sender, RoutedEventArgs e) => SetTicks(false);

    /// <summary>
    /// Ticks or clears every row that is still allowed one — an already-awarded row
    /// is left alone, exactly as its disabled checkbox says.
    /// </summary>
    private void SetTicks(bool ticked)
    {
        foreach (var row in _shown.Where(r => r.CanBeAwarded)) row.IsSelected = ticked;

        // IsSelected raises no change notification, so the grid is told to redraw.
        dgRanking.ItemsSource = null;
        BindPage();
    }

    private void UpdateTickedCount()
    {
        var count = _shown.Count(r => r.IsSelected && r.CanBeAwarded);
        tbTicked.Text = count == 0 ? "nobody ticked" : $"{count} student(s) ticked";
    }

    private void BtnAward_Click(object sender, RoutedEventArgs e)
    {
        // Distinct: a student enrolled in two classes has two rows here, and both
        // may be ticked, but one semester earns one award.
        var studentIds = _shown
            .Where(r => r.IsSelected && r.CanBeAwarded)
            .Select(r => r.StudentId)
            .Distinct()
            .ToList();

        if (studentIds.Count == 0)
        {
            Warn("Tick at least one student to award.");
            return;
        }

        if (!decimal.TryParse(txtAmount.Text.Trim(), out var amount) || amount <= 0)
        {
            Warn("The award amount has to be a number greater than 0.");
            return;
        }

        var note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();

        // Spelt out in full, because this is the one action on this screen that
        // moves money and cannot be taken back.
        var confirm = MessageBox.Show(
            $"Pay {amount:N0} đ to each of {studentIds.Count} student(s)?\n\n"
            + $"Total: {amount * studentIds.Count:N0} đ\n"
            + "The money goes straight into their wallets and cannot be taken back here.",
            "Confirm award", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var result = _awardService.AwardMany(
                studentIds, _shownSemesterId, amount, _shownThreshold, _currentUser.Id, note);

            ShowResult(result, amount);

            // Re-run the search so the Awarded column and the tick boxes catch up.
            BtnShow_Click(sender, e);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot award", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not award:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Reports both halves. A batch can half-succeed — somebody else may have
    /// awarded a student a minute ago — and saying only "12 paid" would hide that.
    /// </summary>
    private static void ShowResult(AwardBatchResult result, decimal amount)
    {
        var message = result.Paid.Count == 0
            ? "Nobody was awarded."
            : $"{result.Paid.Count} student(s) awarded {amount:N0} đ each.";

        if (result.Refused.Count > 0)
        {
            message += $"\n\n{result.Refused.Count} skipped:\n"
                     + string.Join("\n", result.Refused.Distinct().Take(10));
        }

        MessageBox.Show(message, "Award finished", MessageBoxButton.OK,
            result.Refused.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private static void Warn(string message) =>
        MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
}
