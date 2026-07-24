using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  SemesterWindow — list of semesters; editing happens in SemesterDialog.
//  CONTENTS:
//    1. Fields & load     — semesters + class counts into the grid
//    2. Current banner    — which semester contains today, and its phase
//    3. Add / edit / delete — SemesterDialog; edit is locked once the semester
//                             leaves SETUP, delete surfaces the service guard
//    4. SemesterRow       — grid-facing view model (status badge fields)
//
//  There is no "set active" action: the current semester is derived from
//  today's date (see Semester.IsActive), so activeness is not something the
//  admin toggles. Overlap is what would make that ambiguous, and
//  SemesterService rejects it on save.
// ============================================================
public partial class SemesterWindow : Window
{
    private readonly ISemesterService _service = new SemesterService();
    private readonly IClassService _classService = new ClassService();

    private List<SemesterRow> _all = new();

    public SemesterWindow()
    {
        InitializeComponent();
        LoadData();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadData()
    {
        _all = _service.GetAll()
            .Select(s => new SemesterRow(s, _classService.GetBySemesterId(s.SemesterId).Count))
            .ToList();

        emptyState.Visibility = _all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateCurrentBanner();
        BindPage();
    }

    private void BindPage() => dgSemesters.ItemsSource = pager.Slice(_all);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    // ---- 2. Current banner -------------------------------------
    private void UpdateCurrentBanner()
    {
        var current = _service.GetActive();

        if (current == null)
        {
            // Legitimate state: today sits in a gap between two semesters.
            tbCurrent.Text = "None — today falls between semesters";
            phaseBadge.Visibility = Visibility.Collapsed;
            return;
        }

        tbCurrent.Text = current.Name;
        tbPhase.Text = _service.GetPhase(current) switch
        {
            Phase.SETUP => "SETUP",
            Phase.LEARNING => "TEACHING",
            _ => "COMPLETED"
        };
        phaseBadge.Visibility = Visibility.Visible;
    }

    // ---- 3. Row actions ----------------------------------------
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (new SemesterDialog { Owner = this }.ShowDialog() == true) LoadData();
    }

    /// <summary>Double-click drills into the semester's classes — the common action.</summary>
    private void DgSemesters_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgSemesters.SelectedItem is SemesterRow) OpenClasses();
    }

    private void BtnClasses_Click(object sender, RoutedEventArgs e) => OpenClasses();

    private void OpenClasses()
    {
        if (dgSemesters.SelectedItem is not SemesterRow row)
        {
            MessageBox.Show("Please select a semester to open its classes.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new SemesterClassesWindow(row.Semester.SemesterId) { Owner = this }.ShowDialog();
        LoadData(); // class counts may have changed
    }

    /// <summary>Only a semester still in SETUP can be edited, so the button follows the selection.</summary>
    private void DgSemesters_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (btnEdit == null) return;

        if (dgSemesters.SelectedItem is not SemesterRow row)
        {
            btnEdit.IsEnabled = true;   // nothing selected — EditSelected() explains
            btnEdit.ToolTip = null;
            return;
        }

        var editable = _service.IsEditable(row.Semester);
        btnEdit.IsEnabled = editable;
        btnEdit.ToolTip = editable ? null : SemesterService.LockedMessage(row.Semester);
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void EditSelected()
    {
        if (dgSemesters.SelectedItem is not SemesterRow row)
        {
            MessageBox.Show("Please select a semester to edit.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Checked here too, not just on the button: the service is the real guard, but
        // saying why up-front beats letting the user fill in a dialog that gets rejected.
        if (!_service.IsEditable(row.Semester))
        {
            MessageBox.Show(SemesterService.LockedMessage(row.Semester), "Cannot edit",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (new SemesterDialog(row.Semester) { Owner = this }.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgSemesters.SelectedItem is not SemesterRow row)
        {
            MessageBox.Show("Please select a semester to delete.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Delete semester \"{row.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _service.Delete(row.Semester.SemesterId);
            LoadData();
        }
        catch (InvalidOperationException ex)
        {
            // Service guard (e.g. the semester still has classes) — message is user-facing.
            MessageBox.Show(ex.Message, "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting semester: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 4. SemesterRow (grid-facing view model) ---------------
    private sealed record SemesterRow(Semester Semester, int ClassCount)
    {
        public string Name => Semester.Name;
        public DateOnly SetupEndDate => Semester.SetupEndDate;

        /// <summary>First day of teaching — the day after setup ends.</summary>
        public DateOnly TeachingFrom => Semester.SetupEndDate.AddDays(1);

        public string RangeText => $"{Semester.StartDate:dd/MM/yyyy} – {Semester.EndDate:dd/MM/yyyy}";

        /// <summary>Drives the badge colour via DataTrigger; keep in sync with StatusText.</summary>
        public string StatusKind
        {
            get
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (today < Semester.StartDate) return "Upcoming";
                if (today > Semester.EndDate) return "Completed";
                return today <= Semester.SetupEndDate ? "Setup" : "Teaching";
            }
        }

        public string StatusText => StatusKind switch
        {
            "Upcoming" => "Upcoming",
            "Setup" => "● Setup",
            "Teaching" => "● Teaching",
            _ => "Completed"
        };
    }
}
