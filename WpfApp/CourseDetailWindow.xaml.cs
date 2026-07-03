using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class CourseDetailWindow : Window
{
    private readonly ICourseService _service = new CourseService();
    private readonly Course? _editCourse;

    public CourseDetailWindow(Course? course = null)
    {
        InitializeComponent();
        _editCourse = course;

        if (course != null)
        {
            Title = "Edit Course";
            txtCode.Text = course.Code;
            txtName.Text = course.Name;
            SelectComboItem(cmbLevel, course.Level);
            SelectComboItem(cmbLanguage, course.Language);
            txtDuration.Text = course.DurationSessions.ToString();
            txtFee.Text = course.TuitionFee.ToString("0.##");
            txtDescription.Text = course.Description ?? "";
            chkActive.IsChecked = course.IsActive;
        }
    }

    private static void SelectComboItem(System.Windows.Controls.ComboBox cmb, string? value)
    {
        if (value == null) return;
        foreach (var item in cmb.Items)
        {
            if (item is System.Windows.Controls.ComboBoxItem ci && ci.Content.ToString() == value)
            {
                cmb.SelectedItem = ci;
                return;
            }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var code = txtCode.Text.Trim();
        var name = txtName.Text.Trim();
        var level = (cmbLevel.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
        var language = (cmbLanguage.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
        var durationText = txtDuration.Text.Trim();
        var feeText = txtFee.Text.Trim();
        var description = txtDescription.Text.Trim();
        var isActive = chkActive.IsChecked ?? true;

        // Validation
        if (string.IsNullOrEmpty(code))
        {
            MessageBox.Show("Code is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrEmpty(language))
        {
            MessageBox.Show("Language is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(durationText, out var duration) || duration <= 0)
        {
            MessageBox.Show("Duration Sessions must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(feeText, out var fee) || fee < 0)
        {
            MessageBox.Show("Tuition Fee must be a valid non-negative number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editCourse == null)
        {
            // Check duplicate code
            if (_service.GetAll().Any(c => c.Code.Equals(code, System.StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Course code already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var course = new Course
            {
                Code = code,
                Name = name,
                Level = level,
                Language = language,
                DurationSessions = duration,
                TuitionFee = fee,
                Description = description,
                IsActive = isActive,
                CreatedAt = System.DateTime.Now
            };
            _service.Save(course);
        }
        else
        {
            // Check duplicate code (excluding self)
            if (_service.GetAll().Any(c => c.Code.Equals(code, System.StringComparison.OrdinalIgnoreCase) && c.CourseId != _editCourse.CourseId))
            {
                MessageBox.Show("Course code already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _editCourse.Code = code;
            _editCourse.Name = name;
            _editCourse.Level = level;
            _editCourse.Language = language;
            _editCourse.DurationSessions = duration;
            _editCourse.TuitionFee = fee;
            _editCourse.Description = description;
            _editCourse.IsActive = isActive;
            _service.Update(_editCourse);
        }

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
