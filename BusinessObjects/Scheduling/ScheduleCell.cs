namespace BusinessObjects;

/// <summary>One cell in the weekly schedule grid — empty, or holding a single session's display info.</summary>
public class ScheduleCell
{
    public bool HasSession { get; set; }
    public int SessionId { get; set; }
    public string ClassName { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string CounterpartName { get; set; } = ""; // teacher name (student view) or course name (teacher view)
    public string TimeDisplay { get; set; } = "";
    public string Status { get; set; } = ""; // e.g. PRESENT/ABSENT/LATE or "Not yet"
}

/// <summary>One row of the grid: a slot number plus one cell per day (Mon..Sun).</summary>
public class ScheduleRow
{
    public int SlotNumber { get; set; }
    public ScheduleCell[] Days { get; set; } = new ScheduleCell[7]; // index 0=Mon ... 6=Sun
}
