using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class ClassSchedule
{
    public int ScheduleId { get; set; }

    public int ClassId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
