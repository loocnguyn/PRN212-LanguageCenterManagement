using BusinessObjects;

namespace Repositories;

// IEnrollmentRepository — repository contract for Enrollment persistence.

public interface IEnrollmentRepository
{
    void EnrollWithInvoice(Enrollment enrollment, decimal tuitionFee, DateOnly dueDate, string note);
    void EnrollWithInvoice(Enrollment enrollment, InvoicePricingInfo pricing, DateOnly dueDate, string note);
    List<Enrollment> GetAll();
    Enrollment? GetById(int id);
    void Save(Enrollment entity);
    void Update(Enrollment entity);
    void Delete(int id);
    List<Enrollment> GetByClassId(int classId);
    List<Enrollment> GetByStudentId(int studentId);
    List<Enrollment> GetAllByStudentId(int studentId);  
    Enrollment? GetByStudentAndClass(int studentId, int classId);
    void TransferClass(int oldEnrollmentId, int newClassId, decimal newTuitionFee, DateOnly dueDate, string note);
    void LockEnrollmentsByClass(int classId);
    int CountByClassId(int classId);
}
