using BusinessObjects;

namespace Repositories;

// ISessionRepository — repository contract for Session persistence.

public interface ISessionRepository
{
    List<Session> GetAll();
    Session? GetById(int id);
    void Save(Session entity);
    void Update(Session entity);
    void Delete(int id);
    List<Session> GetByClassId(int classId);
    List<Session> GetByClassIds(List<int> classIds);
    List<Session> GetByClassIdWithDetails(int classId);
    int CountByClassId(int classId);
    void BulkSave(List<Session> sessions);

    List<Session> GetForRoomEditing(int classId);
    List<Session> GetSessionsInRoomOnDate(int roomId, DateOnly date, int excludeSessionId);
    void ChangeRoom(int sessionId, int? roomId, string? note);
}
