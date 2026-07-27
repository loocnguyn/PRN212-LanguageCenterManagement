using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  SessionRoomWindow — move a single session to another room.
//  CONTENTS:
//    1. Construction & class picker — load classes
//    2. LoadSessions                — that class's sessions into the grid
//    3. Change room                 — dialog + service (conflict-checked)
//    4. View models                 — ClassOption / SessionRow
//  The class's own default room is never touched here — only per-session
//  overrides. Conflict validation lives in SessionService.
// ============================================================
public partial class SessionRoomWindow : Window
{
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IClassService _classService = new ClassService();
    private readonly IClassroomService _classroomService = new ClassroomService();

    // ---- 1. Construction & class picker ------------------------
    public SessionRoomWindow()
    {
        InitializeComponent();
        cboClass.ItemsSource = _classService.GetAll()
            .OrderBy(c => c.Name)
            .Select(c => new ClassOption(c.ClassId, $"{c.Name} — {c.SnapCourseCode}", c.Classroom?.Name))
            .ToList();
    }

    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadSessions();

    // ---- 2. Load sessions --------------------------------------
    private void LoadSessions()
    {
        if (cboClass.SelectedItem is not ClassOption opt)
        {
            dgSessions.ItemsSource = null;
            emptyState.Visibility = Visibility.Visible;
            tbDefaultRoom.Text = "";
            return;
        }

        tbDefaultRoom.Text = string.IsNullOrEmpty(opt.DefaultRoom)
            ? "" : $"Default room: {opt.DefaultRoom}";

        var rows = _sessionService.GetSessionsForRoomEditing(opt.ClassId)
            .Select(s => new SessionRow(s))
            .ToList();

        dgSessions.ItemsSource = rows;
        emptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (rows.Count == 0) emptyState.Text = "This class has no sessions yet.";
    }

    // ---- 3. Change room ----------------------------------------
    private void DgSessions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => BtnChangeRoom_Click(sender, e);

    private void BtnChangeRoom_Click(object sender, RoutedEventArgs e)
    {
        if (dgSessions.SelectedItem is not SessionRow row)
        {
            MessageBox.Show("Please select a session first.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SessionRoomDialog(row.Session, _classroomService.GetAll()) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _sessionService.ChangeSessionRoom(row.Session.SessionId, dialog.SelectedRoomId, dialog.Note);
            LoadSessions();
        }
        catch (InvalidOperationException ex)
        {
            // Room conflict or missing session — the service message is user-facing.
            MessageBox.Show(ex.Message, "Cannot change room", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 4. View models ----------------------------------------
    private sealed record ClassOption(int ClassId, string Display, string? DefaultRoom);

    private sealed record SessionRow(Session Session)
    {
        public string DateText => Session.SessionDate.ToString("dd/MM/yyyy");
        public string DayName => Session.SessionDate.DayOfWeek switch
        {
            DayOfWeek.Monday => "Monday", DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday", DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday", DayOfWeek.Saturday => "Saturday",
            _ => "Sunday"
        };
        public string TimeText => Session.Schedule != null
            ? $"{Session.Schedule.StartTime:hh\\:mm}-{Session.Schedule.EndTime:hh\\:mm}"
            : "—";
        public string RoomName => Session.EffectiveRoomName;
        public bool IsOverridden => Session.HasRoomOverride;
        public string? Note => Session.RoomChangeNote;
    }
}
