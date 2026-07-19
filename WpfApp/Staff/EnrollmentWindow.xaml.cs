using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  EnrollmentWindow — enroll students into classes (with discounts).
//  CONTENTS:
//    1. Construction & setup — active semester, discount options, load
//    2. Enroll               — enroll selected student+class (+discount)
//    3. Change class / Drop   — move or withdraw an enrollment
//    4. Helpers               — discount formatting, DiscountOption record
// ============================================================
public partial class EnrollmentWindow : Window
{
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ISemesterService _semesterService = new SemesterService();
    private readonly ITuitionDiscountService _discountService = new TuitionDiscountService();
    private List<Enrollment> _all = new();
    private Semester? _activeSemester;

    public EnrollmentWindow() { InitializeComponent(); InitializeActiveSemester(); LoadData(); }

    private void InitializeActiveSemester()
    {
        _activeSemester = _semesterService.GetActive();
        if (_activeSemester == null)
        {
            tbActiveSemester.Text = "No active semester";
            tbActivePhase.Text = "";
            return;
        }
        tbActiveSemester.Text = _activeSemester.Name;
        Phase? phase = _semesterService.GetActivePhase();
        tbActivePhase.Text = phase.HasValue ? $"[{phase.Value}]" : "";

        // Load classes for active semester that are UPCOMING or ACTIVE
        var classes = _classService.GetBySemesterId(_activeSemester.SemesterId)
            .Where(c => c.Status == "UPCOMING" || c.Status == "ACTIVE")
            .ToList();
        cboClass.ItemsSource = classes;
        if (classes.Any()) cboClass.SelectedIndex = 0;

        LoadDiscounts();
    }

    private void LoadDiscounts()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var discounts = new List<DiscountOption>
        {
            new(null, "None")
        };
        discounts.AddRange(_discountService.GetActive(today)
            .Select(x => new DiscountOption(
                x.DiscountId,
                $"{x.Code} - {x.Name} ({FormatDiscount(x)})")));
        cboDiscount.ItemsSource = discounts;
        cboDiscount.SelectedIndex = 0;
    }

    private void LoadData()
    {
        // Show all enrollments with student and class details
        _all = _enrollmentService.GetAll();
        dgEnrollments.ItemsSource = _all;
    }

    private void DgEnrollments_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnEnroll_Click(object sender, RoutedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls)
        {
            MessageBox.Show("Please select a class.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(txtStudentId.Text.Trim(), out int studentId))
        {
            MessageBox.Show("Please enter a valid Student ID.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var discountId = (cboDiscount.SelectedItem as DiscountOption)?.DiscountId;
            _enrollmentService.Enroll(studentId, cls.ClassId, discountId);
            MessageBox.Show($"Student {studentId} enrolled successfully in '{cls.Name}'.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
            txtStudentId.Text = "";
            cboDiscount.SelectedIndex = 0;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Enrollment Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        InitializeActiveSemester();
        LoadData();
    }

    private void BtnChangeClass_Click(object sender, RoutedEventArgs e)
    {
        if (dgEnrollments.SelectedItem is not Enrollment en)
        {
            MessageBox.Show("Please select the current enrollment to transfer.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (cboClass.SelectedItem is not Class newClass)
        {
            MessageBox.Show("Please select the target class.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Transfer {en.Student?.FullName ?? $"student #{en.StudentId}"} from '{en.Class?.Name}' to '{newClass.Name}'?\n\n" +
            "If the new class is cheaper, the difference will be refunded to the student's wallet.",
            "Confirm Class Transfer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _enrollmentService.TransferClass(en.EnrollmentId, newClass.ClassId);
            MessageBox.Show("Class transferred successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Transfer Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDrop_Click(object sender, RoutedEventArgs e)
    {
        if (dgEnrollments.SelectedItem is not Enrollment en)
        {
            MessageBox.Show("Please select an enrollment to drop.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var confirm = MessageBox.Show($"Drop enrollment #{en.EnrollmentId} for {en.Student?.FullName}?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm == MessageBoxResult.Yes)
        {
            try
            {
                _enrollmentService.Drop(en.EnrollmentId);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error dropping enrollment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static string FormatDiscount(TuitionDiscount discount)
        => discount.DiscountType == "PERCENT"
            ? $"{discount.DiscountValue:0.##}%"
            : $"{discount.DiscountValue:N0} VND";

    private sealed record DiscountOption(int? DiscountId, string DisplayText);
}
