using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class SemesterWindow : Window
{
    private readonly ISemesterService _service = new SemesterService();
    private List<Semester> _all = new();
    private bool _isLoading;
    private bool _isEditing;

    public SemesterWindow() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        _isLoading = true;
        _all = _service.GetAll();
        dgSemesters.ItemsSource = _all;
        _isLoading = false;
    }

    private void ClearForm()
    {
        _isEditing = false;
        txtName.Text = "";
        dpStartDate.SelectedDate = null;
        dpSetupEndDate.SelectedDate = null;
        dpEndDate.SelectedDate = null;
        chkActive.IsChecked = false;
    }

    private void DpStartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        _isEditing = true;
        if (dpStartDate.SelectedDate is DateTime start)
        {
            var startOnly = DateOnly.FromDateTime(start);
            var setupEnd = startOnly.AddDays(14);
            var end = setupEnd.AddDays(56);
            dpSetupEndDate.SelectedDate = setupEnd.ToDateTime(TimeOnly.MinValue);
            dpEndDate.SelectedDate = end.ToDateTime(TimeOnly.MinValue);
        }
    }

    private void DgSemesters_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isEditing)
        {
            var discard = MessageBox.Show("You have unsaved changes. Discard them?", "Unsaved Changes",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (discard != MessageBoxResult.Yes)
            {
                // Re-select previous item (revert selection)
                _isEditing = false;
                _isLoading = true;
                dgSemesters.SelectedItem = null;
                _isLoading = false;
                return;
            }
        }
        if (dgSemesters.SelectedItem is not Semester s) return;
        _isEditing = false;
        _isLoading = true;
        txtName.Text = s.Name;
        dpStartDate.SelectedDate = s.StartDate.ToDateTime(TimeOnly.MinValue);
        dpSetupEndDate.SelectedDate = s.SetupEndDate?.ToDateTime(TimeOnly.MinValue);
        dpEndDate.SelectedDate = s.EndDate.ToDateTime(TimeOnly.MinValue);
        chkActive.IsChecked = s.IsActive;
        _isLoading = false;
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm(out var semester)) return;
        try
        {
            _service.Save(semester);
            _isEditing = false;
            LoadData();
            ClearForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding semester: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgSemesters.SelectedItem is not Semester existing)
        {
            MessageBox.Show("Please select a semester to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!ValidateForm(out var semester)) return;
        semester.SemesterId = existing.SemesterId;
        try
        {
            _service.Update(semester);
            _isEditing = false;
            LoadData();
            ClearForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating semester: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgSemesters.SelectedItem is not Semester s)
        {
            MessageBox.Show("Please select a semester to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (s.IsActive)
        {
            MessageBox.Show("Cannot delete the active semester.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var confirm = MessageBox.Show($"Delete semester '{s.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        try
        {
            _service.Delete(s.SemesterId);
            LoadData();
            ClearForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting semester: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSetActive_Click(object sender, RoutedEventArgs e)
    {
        if (dgSemesters.SelectedItem is not Semester s)
        {
            MessageBox.Show("Please select a semester to set as active.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            _service.SetActive(s.SemesterId);
            _isEditing = false;
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error setting active semester: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ValidateForm(out Semester semester)
    {
        semester = null!;
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (dpStartDate.SelectedDate is not DateTime start || dpEndDate.SelectedDate is not DateTime end)
        {
            MessageBox.Show("Start Date and End Date are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (start > end)
        {
            MessageBox.Show("Start Date cannot be later than End Date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (dpSetupEndDate.SelectedDate is DateTime setupEndDt)
        {
            var se = DateOnly.FromDateTime(setupEndDt);
            var startOnly = DateOnly.FromDateTime(start);
            var endOnly = DateOnly.FromDateTime(end);
            if (se < startOnly || se > endOnly)
            {
                MessageBox.Show("Setup End Date must be between Start Date and End Date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        semester = new Semester
        {
            Name = txtName.Text.Trim(),
            StartDate = DateOnly.FromDateTime(start),
            EndDate = DateOnly.FromDateTime(end),
            SetupEndDate = dpSetupEndDate.SelectedDate is DateTime setupEnd ? DateOnly.FromDateTime(setupEnd) : null,
            IsActive = chkActive.IsChecked ?? false
        };
        return true;
    }
}
