using BusinessObjects;
using Repositories;

namespace Services;

/// <summary>One meeting a class's weekly schedule would produce, before the course's
/// session count is applied. See SessionService.GetAvailableSessionDates.</summary>
public sealed record PlannedSession(DateOnly Date, int ScheduleId);

// ============================================================
//  SessionService — the concrete class meetings (one row per
//  date a class actually meets). These are AUTO-GENERATED from
//  a class's weekly ClassSchedules once its semester reaches the
//  LEARNING phase; they are never seeded by hand.
//  CONTENTS:
//    1. CRUD & queries              — pass-through to the repo
//    2. GenerateSessionsForClass    — expand weekly slots into dates
//    3. EnsureSessionsForSemester   — bulk generate for a semester
//    4. Date helpers                — DB day -> DayOfWeek, first match
// ============================================================
public class SessionService : ISessionService
{
    // ---- 1. CRUD & queries -------------------------------------
    private readonly ISessionRepository _sessionRepo;
    private readonly IClassRepository _classRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IClassScheduleRepository _scheduleRepo;

    public SessionService() : this(
        new SessionRepository(), new ClassRepository(),
        new SemesterRepository(), new ClassScheduleRepository())
    { }

    // Injectable overload — lets unit tests supply mocked repositories.
    public SessionService(
        ISessionRepository sessionRepo,
        IClassRepository classRepo,
        ISemesterRepository semesterRepo,
        IClassScheduleRepository scheduleRepo)
    {
        _sessionRepo = sessionRepo;
        _classRepo = classRepo;
        _semesterRepo = semesterRepo;
        _scheduleRepo = scheduleRepo;
    }

    public List<Session> GetAll() => _sessionRepo.GetAll();
    public Session? GetById(int id) => _sessionRepo.GetById(id);
    public void Save(Session entity) => _sessionRepo.Save(entity);
    public void Update(Session entity) => _sessionRepo.Update(entity);
    public void Delete(int id) => _sessionRepo.Delete(id);

    public List<Session> GetByClassId(int classId) => _sessionRepo.GetByClassId(classId);
    public List<Session> GetByClassIds(List<int> classIds) => _sessionRepo.GetByClassIds(classIds);
    public List<Session> GetByClassIdWithDetails(int classId) => _sessionRepo.GetByClassIdWithDetails(classId);

    // ---- 2. Plan / generate a single class's sessions ----------
    /// <summary>
    /// Every meeting date the class's weekly schedule yields inside its semester's teaching
    /// window, in chronological order and NOT capped by the course's session count.
    ///
    /// This is the single source of truth for "how many meetings can this schedule produce":
    /// GenerateSessionsForClass takes from the front of this list, and the schedule editor
    /// validates against its length. Keeping both on the same method is what stops validation
    /// approving a schedule that generation then comes up short on.
    /// </summary>
    public List<PlannedSession> GetAvailableSessionDates(int classId)
    {
        var cls = _classRepo.GetById(classId)
            ?? throw new InvalidOperationException($"Class {classId} not found.");

        var semester = _semesterRepo.GetById(cls.SemesterId)
            ?? throw new InvalidOperationException($"Semester {cls.SemesterId} not found.");

        var planned = new List<PlannedSession>();

        // Teaching starts the day after setup ends — see SemesterService.GetPhase.
        var teachingStart = semester.SetupEndDate.AddDays(1);

        foreach (var schedule in _scheduleRepo.GetByClassId(classId))
        {
            var targetDay = MapDayOfWeek(schedule.DayOfWeek);
            var firstDate = FindFirstMatchingDay(teachingStart, targetDay);

            for (var date = firstDate; date <= semester.EndDate; date = date.AddDays(7))
                planned.Add(new PlannedSession(date, schedule.ScheduleId));
        }

        // Chronological across ALL slots, not slot by slot. A class meeting Mon+Wed must
        // alternate Mon, Wed, Mon, Wed — capping a slot-ordered list would otherwise fill
        // the quota with Mondays and drop every Wednesday.
        return planned.OrderBy(p => p.Date).ThenBy(p => p.ScheduleId).ToList();
    }

    /// <summary>
    /// Expands the class's weekly schedule into concrete dated sessions, stopping once the
    /// course's session count (frozen onto the class as SnapDurationSessions) is met.
    /// No-ops if the class already has sessions, so it is safe to call repeatedly.
    /// </summary>
    public void GenerateSessionsForClass(int classId)
    {
        var cls = _classRepo.GetById(classId)
            ?? throw new InvalidOperationException($"Class {classId} not found.");

        if (_sessionRepo.CountByClassId(classId) > 0)
            return; // already generated

        // The course decides how many meetings the class runs; the semester only bounds
        // when they can happen. The schedule editor refuses to leave a class whose
        // schedule cannot reach this count, so falling short here means the semester or
        // schedule was changed afterwards — generate what fits rather than throwing on startup.
        var sessions = GetAvailableSessionDates(classId)
            .Take(cls.SnapDurationSessions)
            .Select(p => new Session
            {
                ClassId = classId,
                ScheduleId = p.ScheduleId,
                SessionDate = p.Date,
                Status = "SCHEDULED"
            })
            .ToList();

        // If BulkSave fails, the CountByClassId guard above prevents re-generation on the
        // next call — no explicit transaction needed.
        _sessionRepo.BulkSave(sessions);
    }

    // ---- 2b. Per-session room change ---------------------------
    public List<Session> GetSessionsForRoomEditing(int classId)
        => _sessionRepo.GetForRoomEditing(classId);

    public void ChangeSessionRoom(int sessionId, int? newRoomId, string? note)
    {
        var session = _sessionRepo.GetById(sessionId)
            ?? throw new InvalidOperationException("This session no longer exists.");

        // Only a real room needs a conflict check; clearing the override never clashes.
        if (newRoomId is int roomId)
        {
            // This session's time window comes from its weekly schedule slot.
            var mySchedule = session.ScheduleId.HasValue
                ? _scheduleRepo.GetById(session.ScheduleId.Value)
                : null;

            foreach (var other in _sessionRepo.GetSessionsInRoomOnDate(roomId, session.SessionDate, sessionId))
            {
                // If either side has no schedule we cannot compare times — treat any
                // same-day, same-room session as a clash rather than risk a double-booking.
                var clash = mySchedule == null || other.Schedule == null
                    || (mySchedule.StartTime < other.Schedule.EndTime
                        && other.Schedule.StartTime < mySchedule.EndTime);

                if (clash)
                    throw new InvalidOperationException(
                        $"That room is already booked on {session.SessionDate:dd/MM/yyyy} by class "
                        + $"'{other.Class?.Name}' at an overlapping time. Choose another room.");
            }
        }

        _sessionRepo.ChangeRoom(sessionId, newRoomId, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
    }

    // ---- 3. Generate for a whole semester ----------------------
    /// <summary>Generates sessions for every class in the semester, but only while the semester
    /// is in its LEARNING phase (today is past setup-end and on/before end). Called on app startup.</summary>
    public void EnsureSessionsForSemester(int semesterId)
    {
        var semester = _semesterRepo.GetById(semesterId)
            ?? throw new InvalidOperationException($"Semester {semesterId} not found.");

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Only generate if phase is LEARNING. The lower bound is exclusive to match
        // SemesterService.GetPhase, which keeps SetupEndDate itself inside SETUP —
        // teaching (and the first session) starts the day after.
        if (today <= semester.SetupEndDate || today > semester.EndDate)
            return;

        var classes = _classRepo.GetBySemesterId(semesterId);
        foreach (var cls in classes)
        {
            if (_sessionRepo.CountByClassId(cls.ClassId) == 0)
                GenerateSessionsForClass(cls.ClassId);
        }
    }

    // ---- 4. Date helpers ---------------------------------------
    /// <summary>Maps the DB day convention (1=Mon .. 7=Sun) to .NET's DayOfWeek (0=Sun .. 6=Sat).</summary>
    private static DayOfWeek MapDayOfWeek(byte dbDay)
    {
        if (dbDay < 1 || dbDay > 7)
            throw new InvalidOperationException($"Invalid DayOfWeek value: {dbDay}. Must be 1-7.");
        return dbDay == 7 ? DayOfWeek.Sunday : (DayOfWeek)dbDay;
    }

    private static DateOnly FindFirstMatchingDay(DateOnly startDate, DayOfWeek targetDay)
    {
        int daysToAdd = ((int)targetDay - (int)startDate.DayOfWeek + 7) % 7;
        return startDate.AddDays(daysToAdd);
    }
}
