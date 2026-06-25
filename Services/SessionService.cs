using BusinessObjects;
using Repositories;

namespace Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _repo = new SessionRepository();

    public List<Session> GetAll() => _repo.GetAll();
    public Session? GetById(int id) => _repo.GetById(id);
    public void Save(Session entity) => _repo.Save(entity);
    public void Update(Session entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


