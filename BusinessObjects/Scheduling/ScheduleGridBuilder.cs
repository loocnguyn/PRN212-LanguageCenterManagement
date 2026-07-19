namespace BusinessObjects;

/// <summary>
/// Builds a FAP-style weekly schedule grid (Slot x Day) from a flat list of sessions.
/// Shared by StudentScheduleWindow and TeacherScheduleWindow so both render identically.
/// </summary>
public static class ScheduleGridBuilder
{
    /// <param name="sessions">Sessions to place on the grid (any week — will be filtered to weekStart..weekStart+6).</param>
    /// <param name="weekStart">Monday of the target week.</param>
    /// <param name="slots">The configured time slots (from the Slots table) — one grid row per slot.</param>
    /// <param name="counterpartNameSelector">Teacher name (student view) or course name (teacher view) for a session.</param>
    /// <param name="statusTextSelector">Resolves the attendance text shown under the time, e.g. "(attended)" / "(Not yet)".</param>
    public static List<ScheduleRow> Build(
        IEnumerable<Session> sessions,
        DateOnly weekStart,
        IReadOnlyList<Slot> slots,
        Func<Session, string> counterpartNameSelector,
        Func<Session, string> statusTextSelector)
    {
        var weekEnd = weekStart.AddDays(6);
        var inWeek = sessions.Where(s => s.SessionDate >= weekStart && s.SessionDate <= weekEnd).ToList();

        var orderedSlots = slots.OrderBy(s => s.SlotNo).ToList();
        var rows = orderedSlots.Select(slot => new ScheduleRow
        {
            SlotNumber = slot.SlotNo,
            Days = Enumerable.Range(0, 7).Select(_ => new ScheduleCell()).ToArray()
        }).ToList();

        // A session sits in the slot whose start time matches its schedule; if a slot's time
        // was edited after the schedule was created, fall back to the slot covering that time.
        int? MatchSlotNo(TimeOnly start) =>
            orderedSlots.FirstOrDefault(s => s.StartTime == start)?.SlotNo
            ?? orderedSlots.FirstOrDefault(s => start >= s.StartTime && start < s.EndTime)?.SlotNo;

        foreach (var session in inWeek)
        {
            if (session.Schedule == null) continue;

            var slotNumber = MatchSlotNo(session.Schedule.StartTime);
            var row = rows.FirstOrDefault(r => r.SlotNumber == slotNumber);
            if (row == null) continue;

            var dayIndex = ((int)session.SessionDate.DayOfWeek + 6) % 7; // Monday=0 ... Sunday=6

            row.Days[dayIndex] = new ScheduleCell
            {
                HasSession = true,
                SessionId = session.SessionId,
                ClassName = session.Class?.Name ?? "",
                RoomName = session.Class?.Classroom?.Name ?? "",
                CounterpartName = counterpartNameSelector(session),
                TimeDisplay = $"{session.Schedule.StartTime:hh\\:mm}-{session.Schedule.EndTime:hh\\:mm}",
                Status = statusTextSelector(session)
            };
        }

        return rows;
    }

    /// <summary>Monday of the week containing the given date.</summary>
    public static DateOnly GetWeekStart(DateOnly anyDateInWeek)
    {
        var offset = ((int)anyDateInWeek.DayOfWeek + 6) % 7; // days since Monday
        return anyDateInWeek.AddDays(-offset);
    }
}
