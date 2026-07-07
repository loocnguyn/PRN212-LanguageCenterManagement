using BusinessObjects;

namespace Repositories;

public interface IAttendanceRepository
{
    List<Attendance> GetAll();
    Attendance? GetById(int id);
    void Save(Attendance entity);
    void Update(Attendance entity);
    void Delete(int id);
    List<Attendance> GetBySessionId(int sessionId);
    void Upsert(Attendance entity);
}


