using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  RewardReviewWindow — pick a semester + course, rank its students
//  by weighted average, and grant a tuition-voucher to those who
//  clear the threshold (admin-driven, one click).
//  CONTENTS:
//    1. Construction & load   — fill semester/course pickers
//    2. Review                — rank candidates into the grid
//    3. Grant                 — create vouchers for eligible students
//    4. Helpers               — input parsing
// ============================================================
public partial class RewardReviewWindow : Window
{
    // ---- 1. Construction & load --------------------------------
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly IRewardService _rewardService = new RewardService();
    private List<RewardCandidate> _candidates = new();

    public RewardReviewWindow()
    {
        InitializeComponent();
        cboSemester.ItemsSource = _semesterService.GetAll();
        cboCourse.ItemsSource = _courseService.GetAll();
        cboSemester.SelectedItem = _semesterService.GetActive() ?? (cboSemester.Items.Count > 0 ? cboSemester.Items[0] : null);
    }

    // ---- 2. Review ---------------------------------------------
    private void BtnReview_Click(object sender, RoutedEventArgs e)
    {
        if (cboSemester.SelectedValue is not int semesterId || cboCourse.SelectedValue is not int courseId)
        {
            MessageBox.Show("Please select both a semester and a course.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!TryParseThreshold(out var threshold)) return;

        _candidates = _rewardService.GetCandidates(semesterId, courseId, threshold);
        pager.Reset();
        BindPage();

        emptyState.Visibility = _candidates.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_candidates.Count == 0) emptyState.Text = "No students found for this course in this semester.";

        var eligible = _candidates.Count(c => c.IsEligible && !c.AlreadyRewarded);
        var rewarded = _candidates.Count(c => c.AlreadyRewarded);
        tbSummary.Text = $"{_candidates.Count} student(s) · {eligible} newly eligible · {rewarded} already rewarded";
        btnGrant.IsEnabled = eligible > 0;
    }

    private void BindPage() => dgCandidates.ItemsSource = pager.Slice(_candidates);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    // ---- 3. Grant ----------------------------------------------
    private void BtnGrant_Click(object sender, RoutedEventArgs e)
    {
        if (cboSemester.SelectedValue is not int semesterId || cboCourse.SelectedValue is not int courseId) return;
        if (!TryParseThreshold(out var threshold)) return;
        if (!TryParsePositive(txtDiscount.Text, "Discount %", out var discountPercent)) return;
        if (discountPercent > 100)
        {
            MessageBox.Show("Discount % cannot exceed 100.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryParsePositive(txtValidDays.Text, "Valid (days)", out var validDaysDec)) return;
        var validDays = (int)validDaysDec;

        var eligible = _rewardService.GetCandidates(semesterId, courseId, threshold)
            .Count(c => c.IsEligible && !c.AlreadyRewarded);
        if (eligible == 0)
        {
            MessageBox.Show("No newly eligible students to reward.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Grant a {discountPercent:0.##}% tuition voucher (valid {validDays} days) to {eligible} student(s)?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var granted = _rewardService.GrantVouchers(semesterId, courseId, threshold, discountPercent, validDays);
            MessageBox.Show($"Granted {granted} voucher(s). They now appear in Tuition Discounts and can be applied to the students' invoices.",
                "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            BtnReview_Click(sender, e); // refresh so granted students flip to "Rewarded"
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not grant vouchers.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 4. Input helpers --------------------------------------
    private bool TryParseThreshold(out decimal threshold)
    {
        if (decimal.TryParse(txtThreshold.Text.Trim(), out threshold) && threshold >= 0) return true;
        MessageBox.Show("Threshold must be a number (e.g. 8.0).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private static bool TryParsePositive(string text, string field, out decimal value)
    {
        if (decimal.TryParse(text.Trim(), out value) && value > 0) return true;
        MessageBox.Show($"{field} must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }
}
