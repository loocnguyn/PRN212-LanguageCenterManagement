using BusinessObjects;
using Repositories;

namespace Services;

// TeacherAttendanceService — business-logic entry point for TeacherAttendance (mostly delegates to the repository).

public class TeacherAttendanceService : ITeacherAttendanceService
{
    private readonly ITeacherAttendanceRepository _repo = new TeacherAttendanceRepository();

    public List<TeacherAttendance> GetAll() => _repo.GetAll();
    public TeacherAttendance? GetById(int id) => _repo.GetById(id);
    public void Save(TeacherAttendance entity) => _repo.Save(entity);
    public void Update(TeacherAttendance entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


