using System.Windows;
using BusinessObjects;

namespace WpfApp;

public partial class GradeTypeDialog : Window
{
    public GradeType Result { get; private set; } = null!;

    private readonly GradeType? _existing;

    /// <param name="remainingWeight">Weight still available before the course reaches 100%
    /// (accounting for the row being edited), shown as a hint.</param>
    public GradeTypeDialog(decimal remainingWeight, GradeType? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        tbHint.Text = $"Remaining weight available: {remainingWeight:0.##}%";

        if (existing != null)
        {
            tbTitle.Text = "Edit grade component";
            txtName.Text = existing.Name;
            txtWeight.Text = existing.WeightPercent.ToString("0.##");
            txtDescription.Text = existing.Description ?? "";
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(txtWeight.Text.Trim(), out var weight) || weight < 0 || weight > 100)
        {
            MessageBox.Show("Weight must be a number between 0 and 100.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = _existing ?? new GradeType();
        Result.Name = name;
        Result.WeightPercent = weight;
        Result.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
