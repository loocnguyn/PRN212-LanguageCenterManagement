using BusinessObjects;
using Repositories;

namespace Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _repo = new AttendanceRepository();

    public List<Attendance> GetAll() => _repo.GetAll();
    public Attendance? GetById(int id) => _repo.GetById(id);
    public void Save(Attendance entity) => _repo.Save(entity);
    public void Update(Attendance entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
    public List<Attendance> GetBySessionId(int sessionId) => _repo.GetBySessionId(sessionId);
    public void Upsert(Attendance entity) => _repo.Upsert(entity);
}


