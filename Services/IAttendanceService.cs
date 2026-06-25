using BusinessObjects;

namespace Services;

public interface IAttendanceService
{
    List<Attendance> GetAll();
    Attendance? GetById(int id);
    void Save(Attendance entity);
    void Update(Attendance entity);
    void Delete(int id);
}


