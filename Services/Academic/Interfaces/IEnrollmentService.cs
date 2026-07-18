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
    void Enroll(int studentId, int classId, int? discountId);
    void TransferClass(int oldEnrollmentId, int newClassId);
    void Drop(int enrollmentId);
    void LockEnrollmentsForSemester(int semesterId);
}
