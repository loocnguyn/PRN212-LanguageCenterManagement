using BusinessObjects;
using Repositories;

namespace Services;

public class SessionService : ISessionService
{
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
