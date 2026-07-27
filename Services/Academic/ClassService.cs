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
    private readonly ITeacherAttendanceRepository _teacherAttendanceRepo = new TeacherAttendanceRepository();
    private readonly ITeacherRepository _teacherRepo = new TeacherRepository();

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
    /// across its whole date range in its room, students are attending, invoices are
    /// raised — so the fields those facts depend on are frozen for the rest of the run:
    /// both dates, the room, and the capacity cannot drop below who is already in.
    ///
    /// What stays editable, and why:
    ///   NAME     — a label. Invoices and grades reference class_id, never the name, so
    ///              fixing a typo breaks nothing and blocking it is pointless friction.
    ///   CAPACITY — upwards only. A class can legitimately take one more student while
    ///              it runs; it can never hold fewer people than are already sitting in it.
    ///   TEACHERS — see SetTeachers. People resign and fall ill mid-course.
    /// </summary>
    public void Update(Class entity)
    {
        var existing = _repo.GetById(entity.ClassId)
            ?? throw new InvalidOperationException($"Class {entity.ClassId} not found.");

        // A finished class is a closed record, not a form. Its marks are final and its
        // invoices are settled, and moving its dates would silently reopen it — which
        // is exactly the bug this rule replaces: nudging the end date of a completed
        // class turned it back into an ONGOING one.
        if (existing.IsFinished)
            throw new InvalidOperationException(
                $"'{existing.Name}' finished on {existing.EndDate:dd/MM/yyyy} and can no longer be edited. " +
                "Create a new class for the next intake.");

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

            // The end date is locked outright, not merely kept in the future. Sessions
            // were generated across the ORIGINAL range and are never regenerated
            // (GenerateSessionsForClass skips a class that already has any): pulling the
            // date in would strand sessions past the end, pushing it out would promise
            // meetings that never get created. Cancel the class instead.
            if (entity.EndDate != existing.EndDate)
                throw new InvalidOperationException(
                    "This class is already running — its end date can no longer be changed. " +
                    "Its sessions were generated for the original date range. " +
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
    /// A class that has already run to its end date cannot be called off after the fact.
    /// </summary>
    public void SetCancelled(int classId, bool cancelled)
    {
        var cls = _repo.GetById(classId);
        if (cls != null && cls.IsFinished)
            throw new InvalidOperationException(
                $"'{cls.Name}' finished on {cls.EndDate:dd/MM/yyyy} — it is too late to cancel it.");

        _repo.SetCancelled(classId, cancelled);
    }

    /// <summary>
    /// Replaces the class's teaching team.
    ///
    /// Teachers CAN change while a class runs — someone resigns, falls ill, or a
    /// substitute takes over, and a class that could never change hands would be
    /// stuck with a teacher who has left. What cannot happen is erasing someone who
    /// actually taught it: their sessions are on record in TeacherAttendances, and
    /// dropping them from the roster would leave that history attached to a teacher
    /// the class claims never to have had.
    /// </summary>
    public void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId)
    {
        if (teacherIds == null || teacherIds.Count == 0)
            throw new InvalidOperationException("A class must keep at least one teacher.");

        var cls = _repo.GetById(classId);
        if (cls != null && cls.IsFinished)
            throw new InvalidOperationException(
                $"'{cls.Name}' finished on {cls.EndDate:dd/MM/yyyy} — its teaching team is part of the record now.");

        var taught = _teacherAttendanceRepo.GetTeacherIdsWithAttendance(classId);
        var removed = taught.Except(teacherIds).ToList();

        if (removed.Count > 0)
        {
            var names = string.Join(", ",
                removed.Select(id => _teacherRepo.GetById(id)?.FullName ?? $"teacher #{id}"));

            throw new InvalidOperationException(
                $"{names} has already taught sessions of this class and cannot be removed from it. " +
                "Add the replacement alongside them, and mark the replacement as primary.");
        }

        _repo.SetTeachers(classId, teacherIds, primaryTeacherId);
    }

    /// <summary>The class's frozen grading structure — read-only by design.</summary>
    public List<ClassGradeComponent> GetGradeComponents(int classId) => _repo.GetGradeComponents(classId);
}
