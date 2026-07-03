using BusinessObjects;

namespace Repositories;

public interface IEnrollmentRepository
{
    List<Enrollment> GetAll();
    Enrollment? GetById(int id);
    void Save(Enrollment entity);
    void Update(Enrollment entity);
    void Delete(int id);
    List<Enrollment> GetByClassId(int classId);
    List<Enrollment> GetByStudentId(int studentId);
    Enrollment? GetByStudentAndClass(int studentId, int classId);
    void LockEnrollmentsByClass(int classId);
    int CountByClassId(int classId);
}
