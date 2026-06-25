using BusinessObjects;

namespace Services;

public interface ITeacherAttendanceService
{
    List<TeacherAttendance> GetAll();
    TeacherAttendance? GetById(int id);
    void Save(TeacherAttendance entity);
    void Update(TeacherAttendance entity);
    void Delete(int id);
}


