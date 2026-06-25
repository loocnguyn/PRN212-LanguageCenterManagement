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
}


