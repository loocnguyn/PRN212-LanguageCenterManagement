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
        if (entity.StartDate.HasValue && entity.StartDate.Value < semester.StartDate)
            throw new InvalidOperationException(
                $"Class cannot start before its semester ({semester.StartDate:dd/MM/yyyy}).");

        if (entity.EndDate.HasValue && entity.EndDate.Value > semester.EndDate)
            throw new InvalidOperationException(
                $"Class cannot end after its semester ({semester.EndDate:dd/MM/yyyy}).");

        return _repo.CreateWithSnapshot(entity, courseId, teacherIds, primaryTeacherId);
    }

    /// <summary>Updates the editable fields. The course snapshot is preserved by the DAO.</summary>
    public void Update(Class entity) => _repo.Update(entity);

    public void Delete(int id) => _repo.Delete(id);
    public void UpdateStatus(int classId, string status) => _repo.UpdateStatus(classId, status);

    public void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId)
    {
        if (teacherIds == null || teacherIds.Count == 0)
            throw new InvalidOperationException("A class must keep at least one teacher.");

        _repo.SetTeachers(classId, teacherIds, primaryTeacherId);
    }

    /// <summary>The class's frozen grading structure — read-only by design.</summary>
    public List<ClassGradeComponent> GetGradeComponents(int classId) => _repo.GetGradeComponents(classId);
}
