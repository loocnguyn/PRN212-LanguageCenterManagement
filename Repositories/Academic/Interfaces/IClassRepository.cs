using BusinessObjects;

namespace Repositories;

// IClassRepository — repository contract for Class persistence.

public interface IClassRepository
{
    List<Class> GetAll();
    Class? GetById(int id);

    /// <summary>Creates a class by freezing a copy of the course onto it. Returns the new id.</summary>
    int CreateWithSnapshot(Class entity, int courseId, IList<int> teacherIds, int? primaryTeacherId);

    /// <summary>Updates the editable fields only — the course snapshot is preserved.</summary>
    void Update(Class entity);

    void Delete(int id);
    List<Class> GetBySemesterId(int semesterId);
    void UpdateStatus(int classId, string status);
    List<Class> GetBySemesterIdWithDetails(int semesterId);

    void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId);
    List<ClassGradeComponent> GetGradeComponents(int classId);
}
