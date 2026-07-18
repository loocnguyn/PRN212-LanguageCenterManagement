using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    public void EnrollWithInvoice(Enrollment enrollment, decimal tuitionFee, DateOnly dueDate, string note)
        => EnrollmentDAO.EnrollWithInvoice(enrollment, tuitionFee, dueDate, note);
    public List<Enrollment> GetAll() => EnrollmentDAO.GetAll();
    public Enrollment? GetById(int id) => EnrollmentDAO.GetById(id);
    public void Save(Enrollment entity) => EnrollmentDAO.Save(entity);
    public void Update(Enrollment entity) => EnrollmentDAO.Update(entity);
    public void Delete(int id) => EnrollmentDAO.Delete(id);
    public List<Enrollment> GetByClassId(int classId) => EnrollmentDAO.GetByClassId(classId);
    public List<Enrollment> GetByStudentId(int studentId) => EnrollmentDAO.GetByStudentId(studentId);
    public Enrollment? GetByStudentAndClass(int studentId, int classId) => EnrollmentDAO.GetByStudentAndClass(studentId, classId);
    public void TransferClass(int oldEnrollmentId, int newClassId, decimal newTuitionFee, DateOnly dueDate, string note)
        => EnrollmentDAO.TransferClass(oldEnrollmentId, newClassId, newTuitionFee, dueDate, note);
    public void LockEnrollmentsByClass(int classId) => EnrollmentDAO.LockEnrollmentsByClass(classId);
    public int CountByClassId(int classId) => EnrollmentDAO.CountByClassId(classId);
}
