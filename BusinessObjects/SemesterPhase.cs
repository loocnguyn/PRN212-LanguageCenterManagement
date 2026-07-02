namespace BusinessObjects;

/// <summary>
/// The phase a semester is currently in, based on today's date:
///   Setup    : start_date .. setup_end_date  (2-week window — Staff assigns classes, enrolls students)
///   Learning : setup_end_date .. end_date     (8-week window — sessions run, attendance/grades)
///   Closed   : after end_date
///   Upcoming : before start_date
/// </summary>
public enum SemesterPhase
{
    Upcoming,
    Setup,
    Learning,
    Closed
}
