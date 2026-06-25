using BusinessObjects;

namespace Repositories;

public interface ISessionRepository
{
    List<Session> GetAll();
    Session? GetById(int id);
    void Save(Session entity);
    void Update(Session entity);
    void Delete(int id);
}


