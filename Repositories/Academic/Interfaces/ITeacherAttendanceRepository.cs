using BusinessObjects;

namespace Repositories;

// ITeacherAttendanceRepository — repository contract for TeacherAttendance persistence.

public interface ITeacherAttendanceRepository
{
    List<TeacherAttendance> GetAll();

    /// <summary>Teachers with a recorded attendance on any session of this class.</summary>
    List<int> GetTeacherIdsWithAttendance(int classId);
    TeacherAttendance? GetById(int id);
    void Save(TeacherAttendance entity);
    void Update(TeacherAttendance entity);
    void Delete(int id);
}


