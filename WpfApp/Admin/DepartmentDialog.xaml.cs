using System.Windows;
using System.Windows.Controls;
using BusinessObjects;

namespace WpfApp;

// ============================================================
//  DepartmentDialog — Add/Edit a department (name + access group).
//  CONTENTS:
//    1. Construction   — add vs edit (prefill name/access group)
//    2. Save / Cancel  — validate name, return via Result
// ============================================================
public partial class DepartmentDialog : Window
{
    public Department Result { get; private set; } = null!;

    private readonly Department? _existing;

    public DepartmentDialog(Department? existing = null)
    {
        InitializeComponent();
        _existing = existing;
        cmbAccess.SelectedIndex = 0;

        if (existing != null)
        {
            tbTitle.Text = "Edit department";
            txtName.Text = existing.Name;
            cmbAccess.SelectedItem = cmbAccess.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => (string)i.Tag == existing.AccessGroup) ?? cmbAccess.Items[0];
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

        var accessGroup = (cmbAccess.SelectedItem as ComboBoxItem)?.Tag as string ?? "ACADEMIC";

        Result = _existing ?? new Department();
        Result.Name = name;
        Result.AccessGroup = accessGroup;

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
