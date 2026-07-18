namespace BusinessObjects;

/// <summary>
/// Fixed daily time slots used to lay out the weekly schedule grid (FAP-style).
/// ClassSchedule.start_time / end_time should be set to match one of these slots
/// so sessions line up correctly in the grid.
/// </summary>
public static class ScheduleSlot
{
    public static readonly IReadOnlyList<(int Number, TimeOnly Start, TimeOnly End)> All = new[]
    {
        (1, new TimeOnly(7, 0),  new TimeOnly(9, 15)),
        (2, new TimeOnly(9, 30), new TimeOnly(11, 45)),
        (3, new TimeOnly(12, 30), new TimeOnly(14, 45)),
        (4, new TimeOnly(15, 0), new TimeOnly(17, 15)),
        (5, new TimeOnly(17, 30), new TimeOnly(19, 45)),
        (6, new TimeOnly(20, 0), new TimeOnly(22, 15)),
    };

    /// <summary>Finds the slot number matching a given start time, or null if no exact match.</summary>
    public static int? FindSlotNumber(TimeOnly startTime)
    {
        foreach (var slot in All)
            if (slot.Start == startTime)
                return slot.Number;
        return null;
    }
}
