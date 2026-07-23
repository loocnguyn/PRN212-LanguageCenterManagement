using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  EnrollmentService — enrollment business rules (capacity, pricing, invoicing).
//  CONTENTS:
//    1. Construction & CRUD   — repos (DI-friendly ctor) + pass-through
//    2. Queries               — by class, by student
//    3. Enroll (+discount)    — public overloads -> EnrollInternal
//    4. TransferClass / Drop  — move or withdraw a student
//    5. LockEnrollmentsForSemester
// ============================================================
public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IClassRepository _classRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ITuitionDiscountRepository _discountRepo;

    // No course repository: pricing comes from the class's frozen snapshot, so
    // enrollment never needs to look at the course at all.

    public EnrollmentService() : this(
        new EnrollmentRepository(), new ClassRepository(), new StudentRepository(),
        new InvoiceRepository(), new TuitionDiscountRepository())
    { }

    // Injectable overload — lets unit tests supply mocked repositories.
    public EnrollmentService(
        IEnrollmentRepository enrollmentRepo,
        IClassRepository classRepo,
        IStudentRepository studentRepo,
        IInvoiceRepository invoiceRepo,
        ITuitionDiscountRepository? discountRepo = null)
    {
        _enrollmentRepo = enrollmentRepo;
        _classRepo = classRepo;
        _studentRepo = studentRepo;
        _invoiceRepo = invoiceRepo;
        _discountRepo = discountRepo ?? new TuitionDiscountRepository();
    }

    public List<Enrollment> GetAll() => _enrollmentRepo.GetAll();
    public Enrollment? GetById(int id) => _enrollmentRepo.GetById(id);
    public void Save(Enrollment entity) => _enrollmentRepo.Save(entity);
    public void Update(Enrollment entity) => _enrollmentRepo.Update(entity);
    public void Delete(int id) => _enrollmentRepo.Delete(id);

    public List<Enrollment> GetByClassId(int classId) => _enrollmentRepo.GetByClassId(classId);
    public List<Enrollment> GetByStudentId(int studentId) => _enrollmentRepo.GetByStudentId(studentId);

    public void Enroll(int studentId, int classId) => EnrollInternal(studentId, classId, null, useDiscountPricing: false);

    public void Enroll(int studentId, int classId, int? discountId) => EnrollInternal(studentId, classId, discountId, useDiscountPricing: true);

    private void EnrollInternal(int studentId, int classId, int? discountId, bool useDiscountPricing)
    {
        var student = _studentRepo.GetById(studentId)
            ?? throw new InvalidOperationException($"Student {studentId} not found.");

        var cls = _classRepo.GetById(classId)
            ?? throw new InvalidOperationException($"Class {classId} not found.");

        // Price comes from the class's frozen snapshot, NOT the course. Repricing a
        // course must never change what already-enrolled students are charged.
        if (cls.SnapTuitionFee <= 0)
            throw new InvalidOperationException(
                $"Class '{cls.Name}' has no valid tuition fee recorded.");

        // Previously this compared against "ACTIVE", which is not a class status at all
        // (they are UPCOMING / ONGOING / COMPLETED / CANCELLED), so no ONGOING class
        // could ever take an enrollment. Open means: not cancelled, not finished.
        if (!cls.IsOpenForEnrollment)
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
                EnrollWithInvoice(existing, cls.SnapTuitionFee, cls.Name, discountId, useDiscountPricing);
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
        EnrollWithInvoice(enrollment, cls.SnapTuitionFee, cls.Name, discountId, useDiscountPricing);
    }

    private void EnrollWithInvoice(
        Enrollment enrollment,
        decimal tuitionFee,
        string className,
        int? discountId,
        bool useDiscountPricing)
    {
        var dueDate = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
        var note = $"Tuition fee for class {className}";

        if (!useDiscountPricing || discountId is null)
        {
            _enrollmentRepo.EnrollWithInvoice(enrollment, tuitionFee, dueDate, note);
            return;
        }

        var pricing = CalculateInvoicePricing(tuitionFee, discountId.Value, enrollment.EnrolledDate);
        var discountNote = pricing.DiscountId.HasValue
            ? $"{note} | Discount applied"
            : note;
        _enrollmentRepo.EnrollWithInvoice(enrollment, pricing, dueDate, discountNote);
    }

    private InvoicePricingInfo CalculateInvoicePricing(decimal originalAmount, int discountId, DateOnly enrolledDate)
    {
        var discount = _discountRepo.GetById(discountId)
            ?? throw new InvalidOperationException($"Discount {discountId} not found.");

        if (!discount.IsActive)
            throw new InvalidOperationException($"Discount '{discount.Code}' is not active.");
        if (discount.StartDate.HasValue && discount.StartDate.Value > enrolledDate)
            throw new InvalidOperationException($"Discount '{discount.Code}' is not available yet.");
        if (discount.EndDate.HasValue && discount.EndDate.Value < enrolledDate)
            throw new InvalidOperationException($"Discount '{discount.Code}' has expired.");

        var discountAmount = discount.DiscountType == "PERCENT"
            ? Math.Round(originalAmount * discount.DiscountValue / 100m, 2)
            : discount.DiscountValue;
        discountAmount = Math.Min(originalAmount, Math.Max(0, discountAmount));

        var status = "ACTIVE";
        DateOnly? deadline = null;
        if (discount.ConditionType == "EARLY_PAYMENT")
        {
            var days = discount.PaymentDeadlineDays ?? 7;
            deadline = enrolledDate.AddDays(days);
        }
        else
        {
            status = "LOCKED";
        }

        return new InvoicePricingInfo
        {
            OriginalAmount = originalAmount,
            DiscountId = discount.DiscountId,
            DiscountAmount = discountAmount,
            FinalAmount = Math.Max(0, originalAmount - discountAmount),
            DiscountDeadline = deadline,
            DiscountStatus = status
        };
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
        if (!newClass.IsOpenForEnrollment)
            throw new InvalidOperationException(
                $"Class '{newClass.Name}' is not open for enrollment (status: {newClass.Status}).");

        // Again the target CLASS's frozen price, not its course's current one.
        if (newClass.SnapTuitionFee <= 0)
            throw new InvalidOperationException(
                $"Class '{newClass.Name}' has no valid tuition fee recorded.");

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
            newClass.SnapTuitionFee,
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
