using System.Windows;
using BusinessObjects;

namespace WpfApp;

// ============================================================
//  DepartmentDialog — Add/Edit a department (name only).
//  CONTENTS:
//    1. Construction   — add vs edit (prefill name)
//    2. Save / Cancel  — validate name, return via Result
//
//  No access group here: which menus a department unlocks is decided in code,
//  see MainWindow.ApplyStaffDepartmentVisibility.
// ============================================================
public partial class DepartmentDialog : Window
{
    public Department Result { get; private set; } = null!;

    private readonly Department? _existing;

    public DepartmentDialog(Department? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        if (existing != null)
        {
            tbTitle.Text = "Edit department";
            txtName.Text = existing.Name;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Department name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = _existing ?? new Department();
        Result.Name = name;

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
