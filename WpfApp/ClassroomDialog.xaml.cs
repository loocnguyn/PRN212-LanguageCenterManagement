using System.Windows;
using BusinessObjects;

namespace WpfApp;

public partial class ClassroomDialog : Window
{
    public Classroom? Result { get; private set; }
    private readonly int? _editId;

    public ClassroomDialog()
    {
        InitializeComponent();
    }

    public ClassroomDialog(Classroom classroom)
    {
        InitializeComponent();
        _editId = classroom.ClassroomId;
        txtName.Text = classroom.Name;
        txtCapacity.Text = classroom.Capacity.ToString();
        txtLocation.Text = classroom.Location ?? "";
        chkIsActive.IsChecked = classroom.IsActive;
    }

    private string? Validate()
    {
        var name = txtName.Text.Trim();
        if (name.Length < 2 || name.Length > 50)
            return "Name must be 2-50 characters.";
        if (!int.TryParse(txtCapacity.Text.Trim(), out var cap) || cap < 1)
            return "Capacity must be an integer >= 1.";
        var loc = txtLocation.Text.Trim();
        if (loc.Length > 100)
            return "Location must be at most 100 characters.";
        return null;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error != null) { MessageBox.Show(error, "Validation"); return; }

        Result = new Classroom
        {
            ClassroomId = _editId ?? 0,
            Name = txtName.Text.Trim(),
            Capacity = int.Parse(txtCapacity.Text.Trim()),
            Location = string.IsNullOrWhiteSpace(txtLocation.Text) ? null : txtLocation.Text.Trim(),
            IsActive = chkIsActive.IsChecked == true
        };
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
