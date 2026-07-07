using BusinessObjects;

namespace Repositories;

public interface IEnrollmentRepository
{
    void EnrollWithInvoice(Enrollment enrollment, decimal tuitionFee, DateOnly dueDate, string note);
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
