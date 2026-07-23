using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  SemesterClassesWindow — the classes of ONE semester.
//  CONTENTS:
//    1. Fields & load       — classes + enrolled counts
//    2. Filter & paging
//    3. Row actions         — add / edit / delete / open enrollments
//    4. ClassRow            — grid-facing view model
//
//  Classes are always created from here, so a class can never be saved without
//  a semester — which the old standalone Class Management screen allowed, and
//  which left classes invisible to every semester-scoped query.
// ============================================================
public partial class SemesterClassesWindow : Window
{
    private readonly IClassService _classService = new ClassService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private readonly int _semesterId;
    private List<ClassRow> _all = new();

    public SemesterClassesWindow(int semesterId)
    {
        InitializeComponent();
        _semesterId = semesterId;

        var semester = _semesterService.GetById(semesterId);
        tbTitle.Text = semester == null ? "Classes" : $"Classes — {semester.Name}";
        tbSubtitle.Text = semester == null
            ? ""
            : $"{semester.StartDate:dd/MM/yyyy} – {semester.EndDate:dd/MM/yyyy}";

        LoadData();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadData()
    {
        _all = _classService.GetClassesWithDetails(_semesterId)
            .Select(c => new ClassRow(c, _enrollmentService.GetByClassId(c.ClassId).Count))
            .OrderBy(r => r.Name)
            .ToList();

        ApplyFilter();
    }

    // ---- 2. Filter & paging ------------------------------------
    private void ApplyFilter()
    {
        if (dgClasses == null) return;

        var kw = txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(r => r.Name.ToLower().Contains(kw)
                              || r.CourseSnapshot.ToLower().Contains(kw)
                              || r.PrimaryTeacherName.ToLower().Contains(kw)).ToList();

        dgClasses.ItemsSource = pager.Slice(filtered);
        emptyState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ApplyFilter();
    private void BtnSearch_Click(object sender, RoutedEventArgs e) { pager.Reset(); ApplyFilter(); }
    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; pager.Reset(); ApplyFilter(); }

    // ---- 3. Row actions ----------------------------------------
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (new ClassDetailWindow(_semesterId) { Owner = this }.ShowDialog() == true) LoadData();
    }

    private void DgClasses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgClasses.SelectedItem is ClassRow) EditSelected();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void EditSelected()
    {
        if (Selected() is not ClassRow row) return;

        // Re-fetch so the dialog gets the teacher assignments Included.
        var cls = _classService.GetById(row.ClassId);
        if (cls == null) { MessageBox.Show("This class no longer exists.", "Info"); LoadData(); return; }

        if (new ClassDetailWindow(cls) { Owner = this }.ShowDialog() == true) LoadData();
    }

    private void BtnEnrollments_Click(object sender, RoutedEventArgs e)
    {
        if (Selected() is not ClassRow row) return;

        new ClassEnrollmentWindow(row.ClassId) { Owner = this }.ShowDialog();
        LoadData(); // enrolled counts may have moved
    }

    /// <summary>
    /// Cancels or reinstates the selected class. UPCOMING / ONGOING / COMPLETED are not
    /// offered anywhere: they follow the class's dates, so the only status left for a
    /// human to decide is whether the class is called off.
    /// </summary>
    private void BtnCancelClass_Click(object sender, RoutedEventArgs e)
    {
        if (Selected() is not ClassRow row) return;

        var cancelling = !row.Class.IsCancelled;
        var verb = cancelling ? "Cancel" : "Reinstate";

        var confirm = MessageBox.Show(
            $"{verb} class \"{row.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _classService.SetCancelled(row.ClassId, cancelling);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not update the class:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected() is not ClassRow row) return;

        if (row.EnrolledCount > 0)
        {
            MessageBox.Show(
                $"'{row.Name}' still has {row.EnrolledCount} enrolled student(s).\n" +
                "Drop them first — deleting would orphan their invoices and grades.",
                "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Delete class \"{row.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _classService.Delete(row.ClassId);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete the class:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private ClassRow? Selected()
    {
        if (dgClasses.SelectedItem is ClassRow row) return row;
        MessageBox.Show("Please select a class.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    // ---- 4. ClassRow -------------------------------------------
    private sealed record ClassRow(Class Class, int EnrolledCount)
    {
        public int ClassId => Class.ClassId;
        public string Name => Class.Name;
        public string Status => Class.Status;
        public string RoomName => Class.Classroom?.Name ?? "";

        /// <summary>What the class was frozen from — not the course's current values.</summary>
        public string CourseSnapshot =>
            $"{Class.SnapCourseCode} — {Class.SnapCourseName}"
            + (string.IsNullOrEmpty(Class.SnapLevel) ? "" : $" ({Class.SnapLevel})");

        public string FeeDisplay => $"{Class.SnapTuitionFee:N0} đ";
        public string EnrolledDisplay => $"{EnrolledCount} / {Class.MaxStudents}";

        public string PrimaryTeacherName => Class.PrimaryTeacher?.FullName ?? "— unassigned —";

        /// <summary>"+2 more" when the class is co-taught; empty otherwise.</summary>
        public string OtherTeachers
        {
            get
            {
                var others = Class.Teachers.Skip(1).Select(t => t.FullName).ToList();
                return others.Count == 0 ? "" : "+ " + string.Join(", ", others);
            }
        }
    }
}
