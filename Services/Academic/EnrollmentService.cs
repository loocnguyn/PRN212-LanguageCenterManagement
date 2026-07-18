using BusinessObjects;
using Repositories;

namespace Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IClassRepository _classRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICourseRepository _courseRepo;

    public EnrollmentService() : this(
        new EnrollmentRepository(), new ClassRepository(), new StudentRepository(),
        new InvoiceRepository(), new CourseRepository())
    { }

    // Injectable overload — lets unit tests supply mocked repositories.
    public EnrollmentService(
        IEnrollmentRepository enrollmentRepo,
        IClassRepository classRepo,
        IStudentRepository studentRepo,
        IInvoiceRepository invoiceRepo,
        ICourseRepository courseRepo)
    {
        _enrollmentRepo = enrollmentRepo;
        _classRepo = classRepo;
        _studentRepo = studentRepo;
        _invoiceRepo = invoiceRepo;
        _courseRepo = courseRepo;
    }

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

        var course = _courseRepo.GetById(cls.CourseId)
            ?? throw new InvalidOperationException($"Course {cls.CourseId} not found.");
        if (course.TuitionFee <= 0)
            throw new InvalidOperationException(
                $"Course '{course.Name}' does not have a valid tuition fee.");

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
                _enrollmentRepo.EnrollWithInvoice(
                    existing,
                    course.TuitionFee,
                    DateOnly.FromDateTime(DateTime.Today).AddMonths(1),
                    $"Tuition fee for class {cls.Name}");
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
        _enrollmentRepo.EnrollWithInvoice(
            enrollment,
            course.TuitionFee,
            DateOnly.FromDateTime(DateTime.Today).AddMonths(1),
            $"Tuition fee for class {cls.Name}");
    }

    public void TransferClass(int oldEnrollmentId, int newClassId)
    {
        var oldEnrollment = _enrollmentRepo.GetById(oldEnrollmentId)
            ?? throw new InvalidOperationException($"Enrollment {oldEnrollmentId} not found.");

        if (oldEnrollment.Status != "ACTIVE")
            throw new InvalidOperationException("Only active enrollments can be transferred.");
        if (oldEnrollment.ClassId == newClassId)
            throw new InvalidOperationException("Please choose a different class to transfer.");

        var newClass = _classRepo.GetById(newClassId)
            ?? throw new InvalidOperationException($"Class {newClassId} not found.");
        if (newClass.Status != "UPCOMING" && newClass.Status != "ACTIVE")
            throw new InvalidOperationException(
                $"Class '{newClass.Name}' is not open for enrollment (status: {newClass.Status}).");

        var newCourse = _courseRepo.GetById(newClass.CourseId)
            ?? throw new InvalidOperationException($"Course {newClass.CourseId} not found.");
        if (newCourse.TuitionFee <= 0)
            throw new InvalidOperationException(
                $"Course '{newCourse.Name}' does not have a valid tuition fee.");

        var existing = _enrollmentRepo.GetByStudentAndClass(oldEnrollment.StudentId, newClassId);
        if (existing != null && existing.Status != "DROPPED")
            throw new InvalidOperationException(
                $"Student {oldEnrollment.StudentId} is already enrolled in class '{newClass.Name}' (status: {existing.Status}).");

        var currentCount = _enrollmentRepo.CountByClassId(newClassId);
        if (currentCount >= newClass.MaxStudents)
            throw new InvalidOperationException($"Class '{newClass.Name}' is full ({currentCount}/{newClass.MaxStudents}).");

        _enrollmentRepo.TransferClass(
            oldEnrollmentId,
            newClassId,
            newCourse.TuitionFee,
            DateOnly.FromDateTime(DateTime.Today).AddMonths(1),
            $"Transferred to class {newClass.Name}");
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
