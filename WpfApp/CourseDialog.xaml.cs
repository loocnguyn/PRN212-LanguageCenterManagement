using System.Windows;
using BusinessObjects;

namespace WpfApp;

public partial class CourseDialog : Window
{
    public Course? Result { get; private set; }
    private readonly int? _editId;

    public CourseDialog()
    {
        InitializeComponent();
    }

    public CourseDialog(Course course)
    {
        InitializeComponent();
        _editId = course.CourseId;
        txtCode.Text = course.Code;
        txtName.Text = course.Name;
        txtLevel.Text = course.Level ?? "";
        txtLanguage.Text = course.Language;
        txtDescription.Text = course.Description ?? "";
        txtSessions.Text = course.DurationSessions.ToString();
        txtFee.Text = course.TuitionFee.ToString("F2");
        chkIsActive.IsChecked = course.IsActive;
    }

    private string? Validate()
    {
        var code = txtCode.Text.Trim();
        if (code.Length < 2 || code.Length > 20)
            return "Code must be 2-20 characters.";
        var name = txtName.Text.Trim();
        if (name.Length < 3 || name.Length > 150)
            return "Name must be 3-150 characters.";
        if (!int.TryParse(txtSessions.Text.Trim(), out var sessions) || sessions < 0)
            return "Sessions must be a non-negative integer.";
        if (!decimal.TryParse(txtFee.Text.Trim(), out var fee) || fee < 0)
            return "Fee must be a non-negative number.";
        return null;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error != null) { MessageBox.Show(error, "Validation"); return; }

        Result = new Course
        {
            CourseId = _editId ?? 0,
            Code = txtCode.Text.Trim(),
            Name = txtName.Text.Trim(),
            Level = string.IsNullOrWhiteSpace(txtLevel.Text) ? null : txtLevel.Text.Trim(),
            Language = string.IsNullOrWhiteSpace(txtLanguage.Text) ? "English" : txtLanguage.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
            DurationSessions = int.Parse(txtSessions.Text.Trim()),
            TuitionFee = decimal.Parse(txtFee.Text.Trim()),
            IsActive = chkIsActive.IsChecked == true,
            CreatedAt = DateTime.Now
        };
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
