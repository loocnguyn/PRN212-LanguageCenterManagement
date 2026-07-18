using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class SessionRepository : ISessionRepository
{
    public List<Session> GetAll() => SessionDAO.GetAll();
    public Session? GetById(int id) => SessionDAO.GetById(id);
    public void Save(Session entity) => SessionDAO.Save(entity);
    public void Update(Session entity) => SessionDAO.Update(entity);
    public void Delete(int id) => SessionDAO.Delete(id);
    public List<Session> GetByClassId(int classId) => SessionDAO.GetByClassId(classId);
    public List<Session> GetByClassIds(List<int> classIds) => SessionDAO.GetByClassIds(classIds);
    public List<Session> GetByClassIdWithDetails(int classId) => SessionDAO.GetByClassIdWithDetails(classId);
    public int CountByClassId(int classId) => SessionDAO.CountByClassId(classId);
    public void BulkSave(List<Session> sessions) => SessionDAO.BulkSave(sessions);
}
