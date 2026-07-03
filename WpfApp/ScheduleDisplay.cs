using System;

namespace WpfApp;

/// <summary>
/// Shared display model for schedule views (Teacher and Student).
/// </summary>
public class ScheduleDisplay
{
    public DateOnly SessionDate { get; set; }
    public string DayName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string TimeDisplay { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string Status { get; set; } = "";
}
