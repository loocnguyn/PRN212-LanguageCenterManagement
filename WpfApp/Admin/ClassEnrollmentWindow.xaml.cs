using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ClassEnrollmentWindow — enrol students into ONE class.
//  CONTENTS:
//    1. Construction & load  — class header, capacity
//    2. Enroll / Drop        — picker dialog; service does the validation
//    3. Paging
//
//  Enrollment happens inside a class rather than on a global screen: the class
//  is what carries the price (its frozen SnapTuitionFee) and the capacity, so
//  both are visible at the moment the decision is made.
//
//  Choosing WHO to enrol, and on which discount, belongs to StudentPickerDialog —
//  this window only shows who is already in and how many seats are left.
// ============================================================
public partial class ClassEnrollmentWindow : Window
{
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();

    private readonly int _classId;
    private Class? _class;
    private List<Enrollment> _enrollments = new();

    public ClassEnrollmentWindow(int classId)
    {
        InitializeComponent();
        _classId = classId;
        LoadAll();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadAll()
    {
        _class = _classService.GetById(_classId);
        if (_class == null)
        {
            MessageBox.Show("This class no longer exists.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
            return;
        }

        tbTitle.Text = _class.Name;
        tbSubtitle.Text = $"{_class.SnapCourseCode} — {_class.SnapCourseName}"
                        + (string.IsNullOrEmpty(_class.SnapLevel) ? "" : $" ({_class.SnapLevel})");

        LoadEnrollments();
    }

    private void LoadEnrollments()
    {
        _enrollments = _enrollmentService.GetByClassId(_classId);

        var active = _enrollments.Count(en => en.Status != "DROPPED");
        statEnrolled.Text = active.ToString();
        statSeats.Text = Math.Max(0, (_class?.MaxStudents ?? 0) - active).ToString();
        statFee.Text = $"{_class?.SnapTuitionFee ?? 0:N0} đ";

        pager.Reset();
        BindPage();
    }

    private void BindPage()
    {
        dgEnrollments.ItemsSource = pager.Slice(_enrollments.Select(en => new EnrollmentRow(en)).ToList());
        emptyState.Visibility = _enrollments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    // ---- 2. Enroll / Drop --------------------------------------
    /// <summary>
    /// Opens the picker. It reports back through EnrolledAnyone rather than DialogResult
    /// alone, because a partly-successful batch still committed real enrollments — treating
    /// that as "cancelled" would leave this window showing stale counts.
    /// </summary>
    private void BtnEnroll_Click(object sender, RoutedEventArgs e)
    {
        if (_class == null) return;

        var picker = new StudentPickerDialog(_class) { Owner = this };
        picker.ShowDialog();

        if (picker.EnrolledAnyone) LoadEnrollments();
    }

    private void BtnDrop_Click(object sender, RoutedEventArgs e)
    {
        if (dgEnrollments.SelectedItem is not EnrollmentRow row)
        {
            MessageBox.Show("Please select an enrollment to drop.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Drop {row.StudentName} from '{_class?.Name}'?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _enrollmentService.Drop(row.EnrollmentId);
            LoadEnrollments();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not drop the enrollment:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadAll();

    private sealed record EnrollmentRow(Enrollment Enrollment)
    {
        public int EnrollmentId => Enrollment.EnrollmentId;
        public string StudentName => Enrollment.Student?.FullName ?? $"Student #{Enrollment.StudentId}";
        public string StudentIdDisplay => $"Student #{Enrollment.StudentId}";
        public DateOnly EnrolledDate => Enrollment.EnrolledDate;
        public string Status => Enrollment.Status;
    }
}
