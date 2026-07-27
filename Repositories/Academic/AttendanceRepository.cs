using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// AttendanceRepository — thin pass-through from the service layer to AttendanceDAO.

public class AttendanceRepository : IAttendanceRepository
{
    public List<Attendance> GetAll() => AttendanceDAO.GetAll();
    public Attendance? GetById(int id) => AttendanceDAO.GetById(id);
    public void Save(Attendance entity) => AttendanceDAO.Save(entity);
    public void Update(Attendance entity) => AttendanceDAO.Update(entity);
    public void Delete(int id) => AttendanceDAO.Delete(id);
    public List<Attendance> GetBySessionId(int sessionId) => AttendanceDAO.GetBySessionId(sessionId);
    public void Upsert(Attendance entity) => AttendanceDAO.Upsert(entity);
    public List<Attendance> GetByStudentId(int studentId) => AttendanceDAO.GetByStudentId(studentId);
    public void BulkUpsert(List<Attendance> entities) => AttendanceDAO.BulkUpsert(entities);
}