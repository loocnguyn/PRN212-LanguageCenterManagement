using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// ClassRepository — thin pass-through from the service layer to ClassDAO.

public class ClassRepository : IClassRepository
{
    public List<Class> GetAll() => ClassDAO.GetAll();
    public Class? GetById(int id) => ClassDAO.GetById(id);

    public int CreateWithSnapshot(Class entity, int courseId, IList<int> teacherIds, int? primaryTeacherId)
        => ClassDAO.CreateWithSnapshot(entity, courseId, teacherIds, primaryTeacherId);

    public void Update(Class entity) => ClassDAO.Update(entity);
    public void Delete(int id) => ClassDAO.Delete(id);
    public List<Class> GetBySemesterId(int semesterId) => ClassDAO.GetBySemesterId(semesterId);
    public void SetCancelled(int classId, bool cancelled) => ClassDAO.SetCancelled(classId, cancelled);
    public List<Class> GetBySemesterIdWithDetails(int semesterId) => ClassDAO.GetBySemesterIdWithDetails(semesterId);

    public void SetTeachers(int classId, IList<int> teacherIds, int? primaryTeacherId)
        => ClassDAO.SetTeachers(classId, teacherIds, primaryTeacherId);

    public List<ClassGradeComponent> GetGradeComponents(int classId) => ClassDAO.GetGradeComponents(classId);

    public List<Class> GetClassesForTeacher(int teacherId, int semesterId) => ClassDAO.GetClassesForTeacher(teacherId, semesterId);

    public List<Course> GetCoursesForTeacher(int teacherId, int semesterId) => ClassDAO.GetCoursesForTeacher(teacherId, semesterId);
}
