using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ClassDetailWindow — create or edit a class inside a semester.
//  CONTENTS:
//    1. Construction   — add (semester given) vs edit
//    2. Course preview — shows what will be frozen onto the class
//    3. Save           — Create() snapshots; Update() edits the rest
//    4. TeacherPick    — checkbox + primary radio row
//
//  On create, the class takes a frozen copy of the course (price, duration,
//  language/level, grading structure). On edit the course picker is locked:
//  swapping it would leave the snapshot describing a different course than the
//  one invoices were raised against.
// ============================================================
public partial class ClassDetailWindow : Window
{
    private readonly IClassService _classService = new ClassService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassroomService _classroomService = new ClassroomService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private readonly Class? _editClass;
    private readonly int _semesterId;
    private List<TeacherPick> _teacherPicks = new();

    /// <summary>Add mode — the class belongs to <paramref name="semesterId"/>.</summary>
    public ClassDetailWindow(int semesterId)
    {
        InitializeComponent();
        _semesterId = semesterId;

        LoadPickers();

        var semester = _semesterService.GetById(semesterId);
        tbSubtitle.Text = semester == null
            ? "Freezes a copy of the course onto the class"
            : $"In {semester.Name} — freezes a copy of the course onto the class";

        // Default the run to the semester's teaching window.
        if (semester != null)
        {
            dpStartDate.SelectedDate = semester.SetupEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
            dpEndDate.SelectedDate = semester.EndDate.ToDateTime(TimeOnly.MinValue);
        }

        // The status row stays hidden: a class being created has no status to show yet,
        // and there was never anything to choose — it follows the dates.
    }

    /// <summary>Edit mode.</summary>
    public ClassDetailWindow(Class classEntity)
    {
        InitializeComponent();
        _editClass = classEntity;
        _semesterId = classEntity.SemesterId;

        LoadPickers();

        Title = "Edit Class";
        tbTitle.Text = $"Edit “{classEntity.Name}”";
        tbSubtitle.Text = $"{classEntity.SnapCourseCode} — {classEntity.SnapCourseName} (frozen)";

        txtName.Text = classEntity.Name;
        cmbCourse.SelectedValue = classEntity.CourseId;
        cmbClassroom.SelectedValue = classEntity.ClassroomId;
        txtMaxStudents.Text = classEntity.MaxStudents.ToString();

        dpStartDate.SelectedDate = classEntity.StartDate.ToDateTime(TimeOnly.MinValue);
        dpEndDate.SelectedDate = classEntity.EndDate.ToDateTime(TimeOnly.MinValue);

        // Shown, not editable: the status is whatever the dates say it is.
        lblStatus.Visibility = Visibility.Visible;
        statusPanel.Visibility = Visibility.Visible;
        tbStatus.Text = classEntity.Status;
        statusBadge.Background = (System.Windows.Media.Brush)FindResource(classEntity.Status switch
        {
            "ONGOING" => "SecondaryBrush",
            "COMPLETED" => "TextSecondaryBrush",
            "CANCELLED" => "DangerBrush",
            _ => "PrimaryBrush"
        });

        // A running class has already produced sessions, attendance and invoices, so the
        // fields those hang off are locked for the rest of the run (ClassService.Update
        // enforces the same rules — this only saves the user from typing into a dead end).
        if (classEntity.IsFinished)
        {
            // Finished: the whole form is a read-only record. Opened rather than
            // refused so the details can still be looked up.
            Title = "Class details";
            tbTitle.Text = $"“{classEntity.Name}” — finished";
            MakeReadOnly();
            tbStatusNote.Text = $"finished on {classEntity.EndDate:dd/MM/yyyy} — this class is a "
                              + "closed record and can no longer be edited";
        }
        else if (classEntity.Status == "ONGOING")
        {
            dpStartDate.IsEnabled = false;
            dpEndDate.IsEnabled = false;
            cmbClassroom.IsEnabled = false;
            tbStatusNote.Text = "class already running — dates and room are locked; capacity "
                              + "cannot drop below the students enrolled, and a teacher who has "
                              + "taught it cannot be removed";
        }

        // The snapshot is immutable — show it, don't offer to change it.
        cmbCourse.IsEnabled = false;
        ShowSnapshot(
            $"Frozen from {classEntity.SnapCourseCode}",
            $"{classEntity.SnapTuitionFee:N0} đ · {classEntity.SnapDurationSessions} sessions · "
            + $"{classEntity.SnapLanguage}{(string.IsNullOrEmpty(classEntity.SnapLevel) ? "" : " " + classEntity.SnapLevel)}"
            + "\nThis copy cannot be edited; create a new class to use different terms.");

        foreach (var ct in classEntity.ClassTeachers)
        {
            var pick = _teacherPicks.FirstOrDefault(p => p.TeacherId == ct.TeacherId);
            if (pick == null) continue;
            pick.IsSelected = true;
            pick.IsPrimary = ct.IsPrimary;
        }
    }

    /// <summary>
    /// Turns the dialog into a viewer: every input dead, Save gone, Cancel becomes
    /// Close. ClassService refuses the write anyway — this is so nobody types a page
    /// of changes before finding that out.
    /// </summary>
    private void MakeReadOnly()
    {
        txtName.IsReadOnly = true;
        txtMaxStudents.IsReadOnly = true;
        cmbClassroom.IsEnabled = false;
        dpStartDate.IsEnabled = false;
        dpEndDate.IsEnabled = false;
        lstTeachers.IsEnabled = false;

        btnSave.Visibility = Visibility.Collapsed;
        btnCancel.Content = "Close";
    }

    private void LoadPickers()
    {
        cmbCourse.ItemsSource = _courseService.GetAll().Where(c => c.IsActive).ToList();
        cmbClassroom.ItemsSource = _classroomService.GetAll();

        _teacherPicks = _teacherService.GetAll()
            .OrderBy(t => t.FullName)
            .Select(t => new TeacherPick(t))
            .ToList();
        lstTeachers.ItemsSource = _teacherPicks;
    }

    // ---- 2. Course preview -------------------------------------
    private void CmbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editClass != null) return; // locked while editing

        if (cmbCourse.SelectedItem is not Course course)
        {
            snapshotPreview.Visibility = Visibility.Collapsed;
            return;
        }

        ShowSnapshot(
            "This will be frozen onto the class",
            $"{course.TuitionFee:N0} đ · {course.DurationSessions} sessions · "
            + $"{course.LanguageName}{(string.IsNullOrEmpty(course.LevelName) ? "" : " " + course.LevelName)}"
            + "\nLater edits to the course will not affect this class.");

        if (string.IsNullOrWhiteSpace(txtName.Text))
            txtName.Text = course.Code + "-";
    }

    private void ShowSnapshot(string title, string body)
    {
        tbSnapshotTitle.Text = title;
        tbSnapshotBody.Text = body;
        snapshotPreview.Visibility = Visibility.Visible;
    }

    private static void SelectComboItem(ComboBox cmb, string? value)
    {
        if (value == null) return;
        foreach (var item in cmb.Items)
        {
            if (item is ComboBoxItem ci && ci.Content?.ToString() == value)
            {
                cmb.SelectedItem = ci;
                return;
            }
        }
    }

    // ---- 3. Save -----------------------------------------------
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name)) { Warn("Class name is required."); return; }

        if (cmbCourse.SelectedValue is not int courseId) { Warn("Please select a course."); return; }
        if (cmbClassroom.SelectedValue is not int classroomId) { Warn("Please select a classroom."); return; }

        if (!int.TryParse(txtMaxStudents.Text.Trim(), out var maxStudents) || maxStudents <= 0)
        {
            Warn("Max students must be a positive whole number.");
            return;
        }

        var room = _classroomService.GetById(classroomId);
        if (room != null && maxStudents > room.Capacity)
        {
            Warn($"Max students ({maxStudents}) exceeds the capacity of {room.Name} ({room.Capacity}).");
            return;
        }

        // No status to collect: it is derived from the dates below.

        var selected = _teacherPicks.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0) { Warn("Assign at least one teacher."); return; }

        var primary = selected.FirstOrDefault(p => p.IsPrimary);
        if (primary == null)
        {
            if (selected.Count == 1)
            {
                primary = selected[0]; // only one candidate — no need to nag
            }
            else
            {
                Warn("Mark one of the selected teachers as primary.");
                return;
            }
        }
        else if (!primary.IsSelected)
        {
            Warn("The primary teacher must also be ticked.");
            return;
        }

        // Both dates are required: the class's status is derived from them, and session
        // generation needs a window to lay meetings out in.
        if (dpStartDate.SelectedDate is not DateTime start || dpEndDate.SelectedDate is not DateTime end)
        {
            Warn("Both the start and end date are required.");
            return;
        }

        var startDate = DateOnly.FromDateTime(start);
        var endDate = DateOnly.FromDateTime(end);

        if (startDate > endDate)
        {
            Warn("Start date cannot be later than end date.");
            return;
        }

        var teacherIds = selected.Select(p => p.TeacherId).ToList();

        try
        {
            if (_editClass == null)
            {
                var entity = new Class
                {
                    SemesterId = _semesterId,
                    Name = name,
                    ClassroomId = classroomId,
                    MaxStudents = maxStudents,
                    StartDate = startDate,
                    EndDate = endDate,
                    CreatedAt = System.DateTime.Now
                };

                // Create() fills the snapshot from the course — never set Snap* here.
                _classService.Create(entity, courseId, teacherIds, primary.TeacherId);
            }
            else
            {
                _editClass.Name = name;
                _editClass.ClassroomId = classroomId;
                _editClass.MaxStudents = maxStudents;
                _editClass.StartDate = startDate;
                _editClass.EndDate = endDate;

                _classService.Update(_editClass);
                _classService.SetTeachers(_editClass.ClassId, teacherIds, primary.TeacherId);
            }

            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot save", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Could not save the class:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void Warn(string message) =>
        MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    // ---- 4. TeacherPick ----------------------------------------
    /// <summary>One row of the teacher list: tick to assign, radio to mark primary.</summary>
    private sealed class TeacherPick : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isPrimary;

        public TeacherPick(Teacher teacher)
        {
            TeacherId = teacher.TeacherId;
            FullName = teacher.FullName;
            Detail = string.Join(" · ", new[] { teacher.Specialization, teacher.Degree }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public int TeacherId { get; }
        public string FullName { get; }
        public string Detail { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                // Un-ticking the primary teacher must not leave a dangling primary.
                if (!value && _isPrimary) IsPrimary = false;
                OnPropertyChanged();
            }
        }

        public bool IsPrimary
        {
            get => _isPrimary;
            set
            {
                if (_isPrimary == value) return;
                _isPrimary = value;
                // Marking someone primary implies they teach the class.
                if (value && !_isSelected) IsSelected = true;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
