using BusinessObjects;

namespace Repositories;

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
}
