using BusinessObjects;

namespace Services;

public interface ISessionService
{
    List<Session> GetAll();
    Session? GetById(int id);
    void Save(Session entity);
    void Update(Session entity);
    void Delete(int id);
}


