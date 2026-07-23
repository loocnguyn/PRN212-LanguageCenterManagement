using BusinessObjects;

namespace Services;

// ISessionService — service contract for Session operations.

public interface ISessionService
{
    List<Session> GetAll();
    Session? GetById(int id);
    void Save(Session entity);
    void Update(Session entity);
    void Delete(int id);
    List<Session> GetByClassId(int classId);
    List<Session> GetByClassIds(List<int> classIds);
    List<Session> GetByClassIdWithDetails(int classId);

    /// <summary>
    /// Every meeting date the class's weekly schedule can produce inside its semester,
    /// uncapped. Its length is how many sessions the schedule can actually deliver —
    /// compare against Class.SnapDurationSessions to see whether a schedule is sufficient.
    /// </summary>
    List<PlannedSession> GetAvailableSessionDates(int classId);

    void GenerateSessionsForClass(int classId);
    void EnsureSessionsForSemester(int semesterId);
}
