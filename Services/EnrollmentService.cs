using BusinessObjects;
using Repositories;

namespace Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepo = new EnrollmentRepository();
    private readonly IClassRepository _classRepo = new ClassRepository();
    private readonly IStudentRepository _studentRepo = new StudentRepository();
    private readonly IInvoiceRepository _invoiceRepo = new InvoiceRepository();

    public List<Enrollment> GetAll() => _enrollmentRepo.GetAll();
    public Enrollment? GetById(int id) => _enrollmentRepo.GetById(id);
    public void Save(Enrollment entity) => _enrollmentRepo.Save(entity);
    public void Update(Enrollment entity) => _enrollmentRepo.Update(entity);
    public void Delete(int id) => _enrollmentRepo.Delete(id);

    public List<Enrollment> GetByClassId(int classId) => _enrollmentRepo.GetByClassId(classId);
    public List<Enrollment> GetByStudentId(int studentId) => _enrollmentRepo.GetByStudentId(studentId);

    public void Enroll(int studentId, int classId)
    {
        var student = _studentRepo.GetById(studentId)
            ?? throw new InvalidOperationException($"Student {studentId} not found.");

        var cls = _classRepo.GetById(classId)
            ?? throw new InvalidOperationException($"Class {classId} not found.");

        if (cls.Status != "UPCOMING" && cls.Status != "ACTIVE")
            throw new InvalidOperationException($"Class '{cls.Name}' is not open for enrollment (status: {cls.Status}).");

        // Check capacity first — DROPPED reactivation must also validate capacity
        int currentCount = _enrollmentRepo.CountByClassId(classId);
        if (currentCount >= cls.MaxStudents)
            throw new InvalidOperationException($"Class '{cls.Name}' is full ({currentCount}/{cls.MaxStudents}).");

        // Check for ANY existing enrollment (ACTIVE, DROPPED, LOCKED) for this student+class pair
        var existing = _enrollmentRepo.GetByStudentAndClass(studentId, classId);
        if (existing != null)
        {
            if (existing.Status == "DROPPED")
            {
                // Reactivate the dropped enrollment
                existing.Status = "ACTIVE";
                existing.EnrolledDate = DateOnly.FromDateTime(DateTime.Today);
                _enrollmentRepo.Update(existing);
                return;
            }
            throw new InvalidOperationException(
                $"Student {studentId} ({student.FullName}) is already enrolled in class '{cls.Name}' (status: {existing.Status}).");
        }

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            ClassId = classId,
            EnrolledDate = DateOnly.FromDateTime(DateTime.Today),
            Status = "ACTIVE"
        };
        _enrollmentRepo.Save(enrollment);
    }

    public void Drop(int enrollmentId)
    {
        var enrollment = _enrollmentRepo.GetById(enrollmentId)
            ?? throw new InvalidOperationException($"Enrollment {enrollmentId} not found.");

        if (enrollment.Status == "DROPPED")
            throw new InvalidOperationException($"Enrollment {enrollmentId} is already dropped.");
        if (enrollment.Status == "LOCKED")
            throw new InvalidOperationException($"Enrollment {enrollmentId} is locked and cannot be dropped.");

        enrollment.Status = "DROPPED";
        _enrollmentRepo.Update(enrollment);

        // Cancel any open invoices tied to this enrollment
        _invoiceRepo.CancelOpenByEnrollmentId(enrollmentId);
    }

    public void LockEnrollmentsForSemester(int semesterId)
    {
        var classes = _classRepo.GetBySemesterId(semesterId);
        foreach (var cls in classes)
        {
            _enrollmentRepo.LockEnrollmentsByClass(cls.ClassId);
        }
    }
}
