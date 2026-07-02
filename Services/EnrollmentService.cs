using BusinessObjects;
using Repositories;

namespace Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepo = new EnrollmentRepository();
    private readonly IClassRepository _classRepo = new ClassRepository();

    public List<Enrollment> GetAll() => _enrollmentRepo.GetAll();
    public Enrollment? GetById(int id) => _enrollmentRepo.GetById(id);
    public void Save(Enrollment entity) => _enrollmentRepo.Save(entity);
    public void Update(Enrollment entity) => _enrollmentRepo.Update(entity);
    public void Delete(int id) => _enrollmentRepo.Delete(id);

    public List<Enrollment> GetByClassId(int classId) => _enrollmentRepo.GetByClassId(classId);

    public void Enroll(int studentId, int classId)
    {
        var cls = _classRepo.GetById(classId)
            ?? throw new InvalidOperationException($"Class {classId} not found.");

        if (cls.Status != "UPCOMING" && cls.Status != "ACTIVE")
            throw new InvalidOperationException($"Class '{cls.Name}' is not open for enrollment (status: {cls.Status}).");

        var existingEnrollments = _enrollmentRepo.GetByClassId(classId);
        if (existingEnrollments.Any(e => e.StudentId == studentId))
            throw new InvalidOperationException($"Student {studentId} is already enrolled in class '{cls.Name}'.");

        int currentCount = _enrollmentRepo.CountByClassId(classId);
        if (currentCount >= cls.MaxStudents)
            throw new InvalidOperationException($"Class '{cls.Name}' is full ({currentCount}/{cls.MaxStudents}).");

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            ClassId = classId,
            EnrolledDate = DateOnly.FromDateTime(DateTime.Today),
            Status = "ACTIVE"
        };
        _enrollmentRepo.Save(enrollment);
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
