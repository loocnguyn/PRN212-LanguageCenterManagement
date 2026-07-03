using BusinessObjects;

namespace Services;

public interface IEnrollmentService
{
    List<Enrollment> GetAll();
    Enrollment? GetById(int id);
    void Save(Enrollment entity);
    void Update(Enrollment entity);
    void Delete(int id);
    List<Enrollment> GetByClassId(int classId);
    List<Enrollment> GetByStudentId(int studentId);
    void Enroll(int studentId, int classId);
    void Drop(int enrollmentId);
    void LockEnrollmentsForSemester(int semesterId);
}
