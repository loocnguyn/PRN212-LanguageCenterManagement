using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Session — domain model.

public partial class Session
{
    public int SessionId { get; set; }

    public int ClassId { get; set; }

    public int? ScheduleId { get; set; }

    public DateOnly SessionDate { get; set; }

    public string? Topic { get; set; }

    public string Status { get; set; } = null!;

    /// <summary>Room override for THIS meeting only. Null = use the class's default classroom.</summary>
    public int? RoomId { get; set; }

    /// <summary>Why this session was moved to a different room (shown to staff/teacher/student).</summary>
    public string? RoomChangeNote { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual ClassSchedule? Schedule { get; set; }

    /// <summary>The overridden room, if any. Needs Include(Room) to be populated.</summary>
    public virtual Classroom? Room { get; set; }

    public virtual ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();

    /// <summary>The room this session actually runs in: the override, else the class's default.
    /// Not mapped — needs Room and Class.Classroom loaded.</summary>
    public string EffectiveRoomName => Room?.Name ?? Class?.Classroom?.Name ?? "";

    /// <summary>True when this session was moved off the class's default room.</summary>
    public bool HasRoomOverride => RoomId != null;
}
