using BusinessObjects;

namespace Services;

// IClassService — service contract for Class operations.

public interface IClassService
{
    List<Class> GetAll();
    Class? GetById(int id);
    List<Class> GetBySemesterId(int semesterId);
    List<Class> GetClassesWithDetails(int semesterId);

    /// <summary>
    /// Creates a class inside a semester, freezing a copy of the course onto it
    /// (price, duration, language/level and grading structure). Returns the new id.
    /// This is the only way to create a class — the snapshot must not be caller-supplied.
    /// </summary>
    int Create(Class entity, int courseId, IList<int> teacherIds, int? primaryTeacherId);

    /// <summary>Updates the editable fields only; the course snapshot is preserved.</summary>
    void Update(Class entity);

    void Delete(int id);
    void UpdateStatus(int classId, string status);

    void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId);

    /// <summary>The class's frozen grading structure.</summary>
    List<ClassGradeComponent> GetGradeComponents(int classId);
}
