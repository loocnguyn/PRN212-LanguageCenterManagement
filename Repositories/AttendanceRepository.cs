using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    public List<Attendance> GetAll() => AttendanceDAO.GetAll();
    public Attendance? GetById(int id) => AttendanceDAO.GetById(id);
    public void Save(Attendance entity) => AttendanceDAO.Save(entity);
    public void Update(Attendance entity) => AttendanceDAO.Update(entity);
    public void Delete(int id) => AttendanceDAO.Delete(id);
}


