namespace BusinessObjects;

/// <summary>
/// A configurable daily time slot (period). Admins can adjust each slot's
/// start/end time via the Slot Time Setting screen; class schedules pick a
/// day + slot, and the weekly timetable grid is laid out from these rows.
/// </summary>
public partial class Slot
{
    public int SlotId { get; set; }

    public int SlotNo { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    /// <summary>Display label, e.g. "Slot 1 (07:00 - 09:15)".</summary>
    public string Display => $"Slot {SlotNo} ({StartTime:HH\\:mm} - {EndTime:HH\\:mm})";
}
