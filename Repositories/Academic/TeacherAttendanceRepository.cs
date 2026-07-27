using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// TeacherAttendanceRepository — thin pass-through from the service layer to TeacherAttendanceDAO.

public class TeacherAttendanceRepository : ITeacherAttendanceRepository
{
    public List<TeacherAttendance> GetAll() => TeacherAttendanceDAO.GetAll();

    public List<int> GetTeacherIdsWithAttendance(int classId)
        => TeacherAttendanceDAO.GetTeacherIdsWithAttendance(classId);
    public TeacherAttendance? GetById(int id) => TeacherAttendanceDAO.GetById(id);
    public void Save(TeacherAttendance entity) => TeacherAttendanceDAO.Save(entity);
    public void Update(TeacherAttendance entity) => TeacherAttendanceDAO.Update(entity);
    public void Delete(int id) => TeacherAttendanceDAO.Delete(id);
}


