using BusinessObjects;

namespace Repositories;

public interface ITeacherAttendanceRepository
{
    List<TeacherAttendance> GetAll();
    TeacherAttendance? GetById(int id);
    void Save(TeacherAttendance entity);
    void Update(TeacherAttendance entity);
    void Delete(int id);
}


