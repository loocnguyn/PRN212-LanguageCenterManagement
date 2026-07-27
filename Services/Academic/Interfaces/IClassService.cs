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

    /// <summary>
    /// Updates the editable fields only; the course snapshot is preserved.
    /// While the class is ONGOING its start date, room and capacity are locked too —
    /// sessions, attendance and invoices already depend on them.
    /// </summary>
    void Update(Class entity);

    void Delete(int id);

    /// <summary>Cancels or reinstates a class. The other statuses follow the dates.</summary>
    void SetCancelled(int classId, bool cancelled);

    void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId);

    /// <summary>The class's frozen grading structure.</summary>
    List<ClassGradeComponent> GetGradeComponents(int classId);
}
