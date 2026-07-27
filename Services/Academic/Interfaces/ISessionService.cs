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

    /// <summary>A class's sessions (with default room, override room and schedule) for the
    /// room-change screen.</summary>
    List<Session> GetSessionsForRoomEditing(int classId);

    /// <summary>
    /// Moves ONE session to another room (or clears the override when newRoomId is null),
    /// recording the reason. Throws InvalidOperationException with a user-facing message if the
    /// target room is already used by another class at an overlapping time that same day.
    /// </summary>
    void ChangeSessionRoom(int sessionId, int? newRoomId, string? note);
}
