using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class GradeRepository : IGradeRepository
{
    public List<Grade> GetAll() => GradeDAO.GetAll();
    public Grade? GetById(int id) => GradeDAO.GetById(id);
    public void Save(Grade entity) => GradeDAO.Save(entity);
    public void Update(Grade entity) => GradeDAO.Update(entity);
    public void Delete(int id) => GradeDAO.Delete(id);
    public List<Grade> GetByEnrollmentId(int enrollmentId) => GradeDAO.GetByEnrollmentId(enrollmentId);
    public List<Grade> GetByEnrollmentIds(List<int> enrollmentIds) => GradeDAO.GetByEnrollmentIds(enrollmentIds);
    public void Upsert(Grade entity) => GradeDAO.Upsert(entity);
    public List<Grade> GetByStudentId(int studentId) => GradeDAO.GetByStudentId(studentId);
}
