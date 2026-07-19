using BusinessObjects;
using Repositories;

namespace Services;

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
    private readonly ISessionRepository _sessionRepo = new SessionRepository();
    private readonly IClassRepository _classRepo = new ClassRepository();
    private readonly ISemesterRepository _semesterRepo = new SemesterRepository();
    private readonly IClassScheduleRepository _scheduleRepo = new ClassScheduleRepository();

    public List<Session> GetAll() => _sessionRepo.GetAll();
    public Session? GetById(int id) => _sessionRepo.GetById(id);
    public void Save(Session entity) => _sessionRepo.Save(entity);
    public void Update(Session entity) => _sessionRepo.Update(entity);
    public void Delete(int id) => _sessionRepo.Delete(id);

    public List<Session> GetByClassId(int classId) => _sessionRepo.GetByClassId(classId);
    public List<Session> GetByClassIds(List<int> classIds) => _sessionRepo.GetByClassIds(classIds);
    public List<Session> GetByClassIdWithDetails(int classId) => _sessionRepo.GetByClassIdWithDetails(classId);

    // ---- 2. Generate a single class's sessions -----------------
    /// <summary>Expands each of the class's weekly schedule slots into concrete dated sessions,
    /// from the day after the semester's setup phase ends through the semester end date.
    /// No-ops if the class already has any sessions (the CountByClassId guard), so it is safe
    /// to call repeatedly.</summary>
    public void GenerateSessionsForClass(int classId)
    {
        var cls = _classRepo.GetById(classId)
            ?? throw new InvalidOperationException($"Class {classId} not found.");

        var semester = _semesterRepo.GetById(cls.SemesterId)
            ?? throw new InvalidOperationException($"Semester {cls.SemesterId} not found.");

        if (semester.SetupEndDate == null)
            throw new InvalidOperationException($"Semester '{semester.Name}' has no SetupEndDate.");

        if (_sessionRepo.CountByClassId(classId) > 0)
            return; // already generated

        var schedules = _scheduleRepo.GetByClassId(classId);
        var sessions = new List<Session>();

        foreach (var schedule in schedules)
        {
            var targetDay = MapDayOfWeek(schedule.DayOfWeek);
            var startDate = semester.SetupEndDate.Value.AddDays(1);
            var firstSessionDate = FindFirstMatchingDay(startDate, targetDay);

            for (var date = firstSessionDate; date <= semester.EndDate; date = date.AddDays(7))
            {
                sessions.Add(new Session
                {
                    ClassId = classId,
                    ScheduleId = schedule.ScheduleId,
                    SessionDate = date,
                    Status = "SCHEDULED"
                });
            }
        }

        try
        {
            _sessionRepo.BulkSave(sessions);
        }
        catch
        {
            // If BulkSave fails, the CountByClassId guard at the top of this method
            // prevents re-generation on the next call — no explicit transaction needed.
            throw;
        }
    }

    // ---- 3. Generate for a whole semester ----------------------
    /// <summary>Generates sessions for every class in the semester, but only while the semester
    /// is in its LEARNING phase (today is past setup-end and on/before end). Called on app startup.</summary>
    public void EnsureSessionsForSemester(int semesterId)
    {
        var semester = _semesterRepo.GetById(semesterId)
            ?? throw new InvalidOperationException($"Semester {semesterId} not found.");

        if (semester.SetupEndDate == null)
            throw new InvalidOperationException($"Semester '{semester.Name}' has no SetupEndDate.");

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Only generate if phase is LEARNING
        if (today < semester.SetupEndDate.Value || today > semester.EndDate)
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
