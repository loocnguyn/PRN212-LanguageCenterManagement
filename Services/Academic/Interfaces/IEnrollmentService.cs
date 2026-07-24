using BusinessObjects;

namespace Services;

// IEnrollmentService — service contract for Enrollment operations.

/// <summary>
/// A student who may still be enrolled into a given class.
/// <paramref name="PreviouslyDropped"/> flags someone who was enrolled before and dropped:
/// they stay offerable because enrolling them again reactivates the old row rather than
/// creating a second one, but the caller should say so rather than presenting them as new.
/// </summary>
public sealed record EnrollableStudent(Student Student, bool PreviouslyDropped);

/// <summary>One student plus the discount chosen for that student specifically.</summary>
public sealed record EnrollRequest(int StudentId, int? DiscountId);

/// <summary>
/// Per-student result of a batch enroll. Failures carry the service's own user-facing
/// message (full class, already enrolled, …) so the caller can report them one by one.
/// </summary>
public sealed record EnrollOutcome(int StudentId, string StudentName, bool Success, string? Error);

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

    /// <summary>
    /// Active students who are not already enrolled in this class. Mirrors the conditions
    /// <see cref="Enroll(int,int,int?)"/> enforces, so nobody is offered who would then be
    /// refused. Does NOT consider remaining seats — capacity is counted per student at the
    /// moment of enrolling, since it can change while the caller is still choosing.
    /// </summary>
    List<EnrollableStudent> GetEnrollableStudents(int classId);

    /// <summary>
    /// Enrolls several students in one call, each with their own discount. Deliberately not
    /// all-or-nothing: one student failing (a full class, a duplicate) must not undo the
    /// others, so every request is attempted and reported individually.
    /// </summary>
    List<EnrollOutcome> EnrollMany(int classId, IList<EnrollRequest> requests);

    /// <summary>
    /// What a student would be billed for this class under a given discount — the same
    /// arithmetic the invoice will use, so a quoted figure cannot drift from the charged one.
    /// </summary>
    decimal PreviewFinalAmount(int classId, int? discountId);

    void TransferClass(int oldEnrollmentId, int newClassId);
    void Drop(int enrollmentId);
    void LockEnrollmentsForSemester(int semesterId);
}
