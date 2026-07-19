using System.Linq;
using System.Windows;
using BusinessObjects;

namespace WpfApp;

public class AttendanceDetailDisplayItem
{
    public string SessionDateDisplay { get; set; } = "";
    public string Topic { get; set; } = "";
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
    public string RecordedAtDisplay { get; set; } = "";
}

// AttendanceDetailWindow — read-only list of a class's attendance rows for the student.
public partial class AttendanceDetailWindow : Window
{
    public AttendanceDetailWindow(string className, List<Attendance> attendances)
    {
        InitializeComponent();
        Title = $"Attendance History — {className}";
        tbClassName.Text = className;

        var displayItems = attendances
            .OrderBy(a => a.Session.SessionDate)
            .Select(a => new AttendanceDetailDisplayItem
            {
                SessionDateDisplay = a.Session.SessionDate.ToString("dd/MM/yyyy"),
                Topic = a.Session.Topic ?? "",
                Status = a.Status,
                Note = a.Note ?? "",
                RecordedAtDisplay = a.RecordedAt.ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();

        dgSessions.ItemsSource = displayItems;
        tbSummary.Text = $"{displayItems.Count} session(s)";
    }
}