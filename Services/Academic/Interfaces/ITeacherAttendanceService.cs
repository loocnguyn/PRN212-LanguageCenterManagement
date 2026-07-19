using BusinessObjects;

namespace Services;

// ITeacherAttendanceService — service contract for TeacherAttendance operations.

public interface ITeacherAttendanceService
{
    List<TeacherAttendance> GetAll();
    TeacherAttendance? GetById(int id);
    void Save(TeacherAttendance entity);
    void Update(TeacherAttendance entity);
    void Delete(int id);
}


