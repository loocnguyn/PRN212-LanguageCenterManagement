using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ClassroomManagementWindow — list & manage classrooms.
//  CONTENTS:
//    1. Fields & load    — all classrooms into the grid
//    2. Search / reset   — filter by keyword
//    3. Paging           — PagerBar slices the filtered list
//    4. Add / edit / delete — open ClassroomDialog; delete selected
// ============================================================
public partial class ClassroomManagementWindow : Window
{
    private readonly IClassroomService _service = new ClassroomService();
    private List<Classroom> _all = new();
    private List<Classroom> _filtered = new();

    public ClassroomManagementWindow() { InitializeComponent(); LoadData(); }
    private void LoadData() { _all = _service.GetAll(); pager.Reset(); ApplyFilter(); }

    private void ApplyFilter()
    {
        var kw = txtSearch.Text.Trim().ToLower();
        _filtered = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(r => r.Name.ToLower().Contains(kw)).ToList();
        dgClassrooms.ItemsSource = pager.Slice(_filtered);
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ApplyFilter();

    private void BtnSearch_Click(object sender, RoutedEventArgs e) { pager.Reset(); ApplyFilter(); }
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; pager.Reset(); ApplyFilter(); }

    // ---- 4. Add / edit / delete --------------------------------
    // Every one of these writes to the database, so every one is wrapped: a
    // duplicate room name or a room still holding classes comes back as an
    // exception, and an unhandled exception in a click handler closes the whole
    // application (App.xaml.cs installs no global handler).
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ClassroomDialog { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result == null) return;

        try
        {
            _service.Save(dialog.Result);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save. The room name may already exist.\n\n{ex.Message}",
                "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgClassrooms.SelectedItem is not Classroom r)
        { MessageBox.Show("Please select a classroom."); return; }

        var dialog = new ClassroomDialog(r) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result == null) return;

        try
        {
            _service.Update(dialog.Result);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save. The room name may already exist.\n\n{ex.Message}",
                "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgClassrooms.SelectedItem is not Classroom r)
        { MessageBox.Show("Please select a classroom."); return; }

        var confirm = MessageBox.Show($"Delete {r.Name}?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _service.Delete(r.ClassroomId);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete this classroom. Classes may still be assigned to it.\n\n{ex.Message}",
                "Delete failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
