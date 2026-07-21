using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  SemesterDialog — create or edit one semester.
//  CONTENTS:
//    1. Construction     — blank (add) or pre-filled (edit)
//    2. Date helpers     — auto-fill on start change, live timeline preview
//    3. Save             — hand the entity to the service, surface its message
//
//  Deliberately thin: every rule (overlap, name clash, setup-date range) lives
//  in SemesterService, so raw-SQL or future callers cannot bypass it. This
//  dialog only checks that the three dates were actually picked.
// ============================================================
public partial class SemesterDialog : Window
{
    private readonly ISemesterService _service = new SemesterService();
    private readonly Semester? _editing;

    /// <summary>Default length of the setup phase when auto-filling from a new start date.</summary>
    private const int DefaultSetupDays = 14;

    /// <summary>Default length of the teaching phase when auto-filling.</summary>
    private const int DefaultLearningDays = 56;

    /// <summary>Add mode.</summary>
    public SemesterDialog()
    {
        InitializeComponent();
        UpdatePreview();
    }

    /// <summary>Edit mode.</summary>
    public SemesterDialog(Semester semester) : this()
    {
        _editing = semester;
        Title = "Edit Semester";
        tbTitle.Text = $"Edit “{semester.Name}”";

        txtName.Text = semester.Name;
        dpStartDate.SelectedDate = semester.StartDate.ToDateTime(TimeOnly.MinValue);
        dpSetupEndDate.SelectedDate = semester.SetupEndDate.ToDateTime(TimeOnly.MinValue);
        dpEndDate.SelectedDate = semester.EndDate.ToDateTime(TimeOnly.MinValue);
        UpdatePreview();
    }

    // ---- 2. Date helpers ---------------------------------------
    private void Dates_Changed(object sender, SelectionChangedEventArgs e)
    {
        // Choosing a start date on a blank form fills plausible defaults; never
        // overwrite dates the user (or an edited row) already has.
        if (sender == dpStartDate
            && dpStartDate.SelectedDate is DateTime start
            && dpSetupEndDate.SelectedDate == null
            && dpEndDate.SelectedDate == null)
        {
            dpSetupEndDate.SelectedDate = start.AddDays(DefaultSetupDays);
            dpEndDate.SelectedDate = start.AddDays(DefaultSetupDays + DefaultLearningDays);
        }

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (tbPreview == null) return;

        if (dpStartDate.SelectedDate is not DateTime s
            || dpSetupEndDate.SelectedDate is not DateTime su
            || dpEndDate.SelectedDate is not DateTime en)
        {
            tbPreview.Text = "Pick a start, setup-end and end date.";
            return;
        }

        var start = DateOnly.FromDateTime(s);
        var setupEnd = DateOnly.FromDateTime(su);
        var end = DateOnly.FromDateTime(en);

        if (setupEnd < start || setupEnd >= end || end <= start)
        {
            tbPreview.Text = "These dates are out of order — setup must end on or after the start "
                           + "and before the end date.";
            return;
        }

        var setupDays = setupEnd.DayNumber - start.DayNumber + 1;
        var learnDays = end.DayNumber - setupEnd.DayNumber;

        tbPreview.Text =
            $"Setup: {start:dd/MM/yyyy} – {setupEnd:dd/MM/yyyy}  ({setupDays} days)\n" +
            $"Teaching: {setupEnd.AddDays(1):dd/MM/yyyy} – {end:dd/MM/yyyy}  ({learnDays} days)\n" +
            $"First class sessions are generated on {setupEnd.AddDays(1):dd/MM/yyyy}.";
    }

    // ---- 3. Save -----------------------------------------------
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (dpStartDate.SelectedDate is not DateTime start
            || dpSetupEndDate.SelectedDate is not DateTime setupEnd
            || dpEndDate.SelectedDate is not DateTime end)
        {
            MessageBox.Show("Start date, setup end date and end date are all required.",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var semester = new Semester
        {
            SemesterId = _editing?.SemesterId ?? 0,
            Name = txtName.Text.Trim(),
            StartDate = DateOnly.FromDateTime(start),
            SetupEndDate = DateOnly.FromDateTime(setupEnd),
            EndDate = DateOnly.FromDateTime(end)
        };

        try
        {
            if (_editing == null) _service.Save(semester);
            else _service.Update(semester);

            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            // Service validation — the message is already written for the user.
            MessageBox.Show(ex.Message, "Cannot save", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error saving the semester:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
