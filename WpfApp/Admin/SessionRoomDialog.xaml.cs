using System.Windows;
using BusinessObjects;

namespace WpfApp;

// ============================================================
//  SessionRoomDialog — pick the room (or "class default") for ONE session.
//  CONTENTS:
//    1. Construction   — build room options, preselect current, show info
//    2. Save / Cancel  — expose the chosen RoomId (null = default) + Reason
//  Validation (room conflicts) lives in SessionService, not here.
// ============================================================
public partial class SessionRoomDialog : Window
{
    /// <summary>Chosen room, or null to use the class's default classroom.</summary>
    public int? SelectedRoomId { get; private set; }

    /// <summary>Reason for the change (null when blank).</summary>
    public string? Note { get; private set; }

    // ---- 1. Construction ---------------------------------------
    public SessionRoomDialog(Session session, List<Classroom> classrooms)
    {
        InitializeComponent();

        var defaultRoom = session.Class?.Classroom?.Name ?? "the class's room";
        var time = session.Schedule != null
            ? $" · {session.Schedule.StartTime:hh\\:mm}-{session.Schedule.EndTime:hh\\:mm}"
            : "";
        tbInfo.Text = $"{session.Class?.Name}  ·  {session.SessionDate:dd/MM/yyyy}{time}";

        var options = new List<RoomOption> { new(null, $"— Use class default room ({defaultRoom}) —") };
        options.AddRange(classrooms.Select(c => new RoomOption(c.ClassroomId, c.Name)));

        cboRoom.ItemsSource = options;
        cboRoom.SelectedItem = options.FirstOrDefault(o => o.Id == session.RoomId) ?? options[0];

        txtNote.Text = session.RoomChangeNote ?? "";
    }

    // ---- 2. Save / Cancel --------------------------------------
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var option = cboRoom.SelectedItem as RoomOption;
        SelectedRoomId = option?.Id;

        // A room change should say why; clearing the override does not need a reason.
        if (SelectedRoomId != null && string.IsNullOrWhiteSpace(txtNote.Text))
        {
            MessageBox.Show("Please give a reason for moving this session to another room.",
                "Reason required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private sealed record RoomOption(int? Id, string Display);
}
