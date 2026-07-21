using System.Windows;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  CourseGradingStructureWindow — edit ONE course's grade components.
//  Opened from GradeTypeManagementWindow by picking a course from the list.
//  CONTENTS:
//    1. Construction & loading   — components for the given course
//    2. UpdateTotalIndicator     — progress bar/status for total weight
//    3. Add / edit / delete      — GradeTypeDialog; enforce 100% total
// ============================================================
public partial class CourseGradingStructureWindow : Window
{
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();
    private readonly Course _course;

    private static readonly Brush AmberBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0x80, 0x00));

    public CourseGradingStructureWindow(Course course)
    {
        InitializeComponent();
        _course = course;
        tbCourseName.Text = course.Name;
        tbCourseCode.Text = $"{course.Code} — configure grade components and their weights";
        LoadGradeTypes();
    }

    private void LoadGradeTypes()
    {
        var gradeTypes = _gradeTypeService.GetByCourseId(_course.CourseId);
        dgGradeTypes.ItemsSource = gradeTypes;
        emptyState.Visibility = gradeTypes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateTotalIndicator(_gradeTypeService.GetTotalWeightPercent(_course.CourseId));
    }

    private void UpdateTotalIndicator(decimal total)
    {
        pbWeight.Value = (double)Math.Min(100, total);

        if (total == 100)
        {
            tbStatus.Text = "✓ Balanced (100%)";
            tbStatus.Foreground = (Brush)FindResource("SecondaryBrush");
            pbWeight.Foreground = (Brush)FindResource("SecondaryBrush");
        }
        else if (total < 100)
        {
            tbStatus.Text = $"{total:0.##}% / 100%  —  {100 - total:0.##}% remaining";
            tbStatus.Foreground = AmberBrush;
            pbWeight.Foreground = AmberBrush;
        }
        else
        {
            tbStatus.Text = $"{total:0.##}% / 100%  —  over by {total - 100:0.##}%";
            tbStatus.Foreground = (Brush)FindResource("DangerBrush");
            pbWeight.Foreground = (Brush)FindResource("DangerBrush");
        }
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var remaining = 100 - _gradeTypeService.GetTotalWeightPercent(_course.CourseId);
        var dlg = new GradeTypeDialog(remaining) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var newTotal = _gradeTypeService.GetTotalWeightPercent(_course.CourseId) + dlg.Result.WeightPercent;
        if (newTotal > 100)
        {
            MessageBox.Show($"Total weight would exceed 100% (would be {newTotal:0.##}%).", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        dlg.Result.CourseId = _course.CourseId;
        try
        {
            _gradeTypeService.Save(dlg.Result);
            LoadGradeTypes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding grade component: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void DgGradeTypes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgGradeTypes.SelectedItem is GradeType) EditSelected();
    }

    private void EditSelected()
    {
        if (dgGradeTypes.SelectedItem is not GradeType gradeType)
        {
            MessageBox.Show("Please select a grade component to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var remaining = 100 - _gradeTypeService.GetTotalWeightPercent(_course.CourseId, gradeType.GradeTypeId);
        var dlg = new GradeTypeDialog(remaining, gradeType) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var newTotal = _gradeTypeService.GetTotalWeightPercent(_course.CourseId, gradeType.GradeTypeId) + dlg.Result.WeightPercent;
        if (newTotal > 100)
        {
            MessageBox.Show($"Total weight would exceed 100% (would be {newTotal:0.##}%).", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _gradeTypeService.Update(dlg.Result);
            LoadGradeTypes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating grade component: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgGradeTypes.SelectedItem is not GradeType gradeType)
        {
            MessageBox.Show("Please select a grade component to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Delete grade component \"{gradeType.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _gradeTypeService.Delete(gradeType.GradeTypeId);
            LoadGradeTypes();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Cannot delete this grade component — it may already have grades recorded against it.\n\n{ex.Message}",
                "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
