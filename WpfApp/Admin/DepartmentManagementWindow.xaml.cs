using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  DepartmentManagementWindow — CRUD for staff departments.
//  CONTENTS:
//    1. Construction & load   — departments into the grid
//    2. Paging                — PagerBar slices the list
//    3. Add / edit / delete   — DepartmentDialog; delete selected
//    4. TrySave               — friendly error on duplicate name
// ============================================================
public partial class DepartmentManagementWindow : Window
{
    private readonly IDepartmentService _service = new DepartmentService();
    private List<Department> _all = new();

    public DepartmentManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData() { _all = _service.GetAll(); BindPage(); }

    private void BindPage() => dgDepartments.ItemsSource = pager.Slice(_all);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DepartmentDialog { Owner = this };
        if (dialog.ShowDialog() == true) TrySave(() => _service.Save(dialog.Result));
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgDepartments.SelectedItem is not Department d)
        { MessageBox.Show("Please select a department."); return; }
        var dialog = new DepartmentDialog(d) { Owner = this };
        if (dialog.ShowDialog() == true) TrySave(() => _service.Update(dialog.Result));
    }

    private void DgDepartments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => BtnEdit_Click(sender, e);

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgDepartments.SelectedItem is not Department d)
        { MessageBox.Show("Please select a department."); return; }
        var confirm = MessageBox.Show($"Delete department \"{d.Name}\"?\nStaff already assigned to it will keep the name but lose the access mapping.",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        try { _service.Delete(d.DepartmentId); LoadData(); }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete this department.\n\n{ex.Message}",
                "Delete failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TrySave(Action save)
    {
        try { save(); LoadData(); }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save. The department name may already exist.\n\n{ex.Message}",
                "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
