using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  ClassService — a class is a run of a course inside a semester.
//
//  The rule this class exists to enforce: a class SNAPSHOTS its course when
//  created (price, duration, language/level, grading structure) and that copy
//  is frozen. Editing a course afterwards must not restate what enrolled
//  students were charged, nor the weights their grades were computed against.
//  Create() is therefore the only way to make a class — plain Save() would let
//  a caller invent snapshot values.
// ============================================================

public class ClassService : IClassService
{
    private readonly IClassRepository _repo = new ClassRepository();
    private readonly ISemesterRepository _semesterRepo = new SemesterRepository();
    private readonly IEnrollmentRepository _enrollmentRepo = new EnrollmentRepository();

    public List<Class> GetAll() => _repo.GetAll();
    public Class? GetById(int id) => _repo.GetById(id);
    public List<Class> GetBySemesterId(int semesterId) => _repo.GetBySemesterId(semesterId);
    public List<Class> GetClassesWithDetails(int semesterId) => _repo.GetBySemesterIdWithDetails(semesterId);

    /// <summary>
    /// Creates a class inside a semester, freezing a copy of the course onto it.
    /// Returns the new class id.
    /// </summary>
    public int Create(Class entity, int courseId, IList<int> teacherIds, int? primaryTeacherId)
    {
        if (teacherIds == null || teacherIds.Count == 0)
            throw new InvalidOperationException("Assign at least one teacher to the class.");

        var semester = _semesterRepo.GetById(entity.SemesterId)
            ?? throw new InvalidOperationException($"Semester {entity.SemesterId} not found.");

        // A class that runs outside its semester would generate sessions outside it too.
        if (entity.StartDate < semester.StartDate)
            throw new InvalidOperationException(
                $"Class cannot start before its semester ({semester.StartDate:dd/MM/yyyy}).");

        if (entity.EndDate > semester.EndDate)
            throw new InvalidOperationException(
                $"Class cannot end after its semester ({semester.EndDate:dd/MM/yyyy}).");

        return _repo.CreateWithSnapshot(entity, courseId, teacherIds, primaryTeacherId);
    }

    /// <summary>
    /// Updates the editable fields. The course snapshot is preserved by the DAO.
    ///
    /// Once a class is ONGOING it has already produced facts — sessions were laid out
    /// from its start date in its room, students are attending, invoices are raised —
    /// so the fields those facts depend on are frozen for the rest of the run. Name and
    /// teachers stay editable; everything blocked here has a proper path elsewhere
    /// (cancel the class, or move a single session's room).
    /// </summary>
    public void Update(Class entity)
    {
        var existing = _repo.GetById(entity.ClassId)
            ?? throw new InvalidOperationException($"Class {entity.ClassId} not found.");

        if (existing.Status == "ONGOING")
        {
            if (entity.StartDate != existing.StartDate)
                throw new InvalidOperationException(
                    "This class is already running — its start date can no longer be changed. " +
                    "Its sessions were laid out from that date.");

            if (entity.ClassroomId != existing.ClassroomId)
                throw new InvalidOperationException(
                    "This class is already running — its room cannot be reassigned. " +
                    "Change the room of the sessions that still lie ahead instead.");

            if (entity.EndDate < DateOnly.FromDateTime(DateTime.Today))
                throw new InvalidOperationException(
                    "A running class cannot be given an end date in the past. " +
                    "Cancel the class if it is not going to finish.");

            var enrolled = _enrollmentRepo.GetByClassId(entity.ClassId)
                .Count(e => e.Status != "DROPPED");
            if (entity.MaxStudents < enrolled)
                throw new InvalidOperationException(
                    $"This class is already running with {enrolled} enrolled student(s); " +
                    $"its capacity cannot be lowered to {entity.MaxStudents}.");
        }

        // The run must stay inside its semester, exactly as Create() requires — an
        // edit is not a way around that.
        var semester = _semesterRepo.GetById(existing.SemesterId);
        if (semester != null)
        {
            if (entity.StartDate < semester.StartDate)
                throw new InvalidOperationException(
                    $"Class cannot start before its semester ({semester.StartDate:dd/MM/yyyy}).");

            if (entity.EndDate > semester.EndDate)
                throw new InvalidOperationException(
                    $"Class cannot end after its semester ({semester.EndDate:dd/MM/yyyy}).");
        }

        _repo.Update(entity);
    }

    public void Delete(int id) => _repo.Delete(id);

    /// <summary>
    /// Cancels or reinstates a class. UPCOMING / ONGOING / COMPLETED are not settable —
    /// they follow the class's dates, so cancellation is the only status a human decides.
    /// </summary>
    public void SetCancelled(int classId, bool cancelled) => _repo.SetCancelled(classId, cancelled);

    public void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId)
    {
        if (teacherIds == null || teacherIds.Count == 0)
            throw new InvalidOperationException("A class must keep at least one teacher.");

        _repo.SetTeachers(classId, teacherIds, primaryTeacherId);
    }

    /// <summary>The class's frozen grading structure — read-only by design.</summary>
    public List<ClassGradeComponent> GetGradeComponents(int classId) => _repo.GetGradeComponents(classId);
}
