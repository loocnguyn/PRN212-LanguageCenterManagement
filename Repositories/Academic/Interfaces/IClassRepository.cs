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
    /// <summary>Cancels or reinstates a class. The other statuses follow the dates.</summary>
    void SetCancelled(int classId, bool cancelled);
    List<Class> GetBySemesterIdWithDetails(int semesterId);

    void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId);
    List<ClassGradeComponent> GetGradeComponents(int classId);
    List<Class> GetClassesForTeacher(int teacherId, int semesterId);
    List<Course> GetCoursesForTeacher(int teacherId, int semesterId);
}
