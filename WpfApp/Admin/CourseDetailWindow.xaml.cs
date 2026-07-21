using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  CourseDetailWindow — add or edit a course.
//  CONTENTS:
//    1. Construction        — add vs edit; prefill and cascade
//    2. Language -> Level   — levels are scoped to the chosen language
//    3. Save                — validate then create/update
//
//  Language and level are picked from the centre's catalogue rather than typed,
//  so a course can never claim a level that language does not offer ("N5" for
//  English). A new course is always created active — the checkbox only appears
//  when editing, since deactivating is a later decision.
// ============================================================
public partial class CourseDetailWindow : Window
{
    private readonly ICourseService _service = new CourseService();
    private readonly ICatalogueService _catalogue = new CatalogueService();
    private readonly Course? _editCourse;

    public CourseDetailWindow(Course? course = null)
    {
        InitializeComponent();
        _editCourse = course;

        cmbLanguage.ItemsSource = _catalogue.GetLanguages();

        if (course == null) return;

        Title = "Edit Course";
        tbTitle.Text = $"Edit “{course.Name}”";

        txtCode.Text = course.Code;
        txtName.Text = course.Name;
        txtDuration.Text = course.DurationSessions.ToString();
        txtFee.Text = course.TuitionFee.ToString("0.##");
        txtDescription.Text = course.Description ?? "";

        // Deactivating is only meaningful for an existing course.
        lblActive.Visibility = Visibility.Visible;
        chkActive.Visibility = Visibility.Visible;
        chkActive.IsChecked = course.IsActive;

        // Setting the language fires the cascade, which fills cmbLevel.
        cmbLanguage.SelectedValue = course.LanguageId;
        if (course.LevelId.HasValue) cmbLevel.SelectedValue = course.LevelId.Value;
    }

    // ---- 2. Language -> Level cascade --------------------------
    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbLevel == null) return; // fires once during InitializeComponent

        if (cmbLanguage.SelectedValue is not int languageId)
        {
            cmbLevel.ItemsSource = null;
            tbLevelHint.Text = "Pick a language first.";
            return;
        }

        var levels = _catalogue.GetLevels(languageId);
        cmbLevel.ItemsSource = levels;
        cmbLevel.SelectedIndex = -1;

        tbLevelHint.Text = levels.Count == 0
            ? "This language has no levels defined yet — the course can be saved without one."
            : "Optional.";
    }

    // ---- 3. Save -----------------------------------------------
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var code = txtCode.Text.Trim();
        var name = txtName.Text.Trim();
        var description = txtDescription.Text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            Warn("Code is required.");
            return;
        }
        if (string.IsNullOrEmpty(name))
        {
            Warn("Name is required.");
            return;
        }
        if (cmbLanguage.SelectedValue is not int languageId)
        {
            Warn("Language is required.");
            return;
        }
        if (!int.TryParse(txtDuration.Text.Trim(), out var duration) || duration <= 0)
        {
            Warn("Duration must be a positive whole number of sessions.");
            return;
        }
        if (!decimal.TryParse(txtFee.Text.Trim(), out var fee) || fee < 0)
        {
            Warn("Tuition fee must be a non-negative number.");
            return;
        }

        var levelId = cmbLevel.SelectedValue as int?;

        var duplicate = _service.GetAll().Any(c =>
            c.Code.Equals(code, System.StringComparison.OrdinalIgnoreCase)
            && (_editCourse == null || c.CourseId != _editCourse.CourseId));
        if (duplicate)
        {
            Warn("Course code already exists.");
            return;
        }

        try
        {
            if (_editCourse == null)
            {
                _service.Save(new Course
                {
                    Code = code,
                    Name = name,
                    LanguageId = languageId,
                    LevelId = levelId,
                    DurationSessions = duration,
                    TuitionFee = fee,
                    Description = description,
                    IsActive = true, // new courses start active
                    CreatedAt = System.DateTime.Now
                });
            }
            else
            {
                _editCourse.Code = code;
                _editCourse.Name = name;
                _editCourse.LanguageId = languageId;
                _editCourse.LevelId = levelId;
                _editCourse.DurationSessions = duration;
                _editCourse.TuitionFee = fee;
                _editCourse.Description = description;
                _editCourse.IsActive = chkActive.IsChecked ?? true;

                // Detach the navigations so EF does not try to re-insert the
                // catalogue rows that were loaded on this instance.
                _editCourse.Language = null!;
                _editCourse.Level = null;

                _service.Update(_editCourse);
            }

            DialogResult = true;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Could not save the course:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void Warn(string message) =>
        MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
