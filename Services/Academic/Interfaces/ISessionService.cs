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
    void GenerateSessionsForClass(int classId);
    void EnsureSessionsForSemester(int semesterId);
}
