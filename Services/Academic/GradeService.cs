using BusinessObjects;
using Repositories;

namespace Services;

// GradeService — business-logic entry point for Grade (mostly delegates to the repository).

public class GradeService : IGradeService
{
    private readonly IGradeRepository _repo = new GradeRepository();

    public List<Grade> GetAll() => _repo.GetAll();
    public Grade? GetById(int id) => _repo.GetById(id);
    public void Save(Grade entity) => _repo.Save(entity);
    public void Update(Grade entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
    public List<Grade> GetByEnrollmentId(int enrollmentId) => _repo.GetByEnrollmentId(enrollmentId);
    public List<Grade> GetByEnrollmentIds(List<int> enrollmentIds) => _repo.GetByEnrollmentIds(enrollmentIds);
    public void Upsert(Grade entity) => _repo.Upsert(entity);
    public List<Grade> GetByStudentId(int studentId) => _repo.GetByStudentId(studentId);
    public void BulkUpsert(List<Grade> entities) => _repo.BulkUpsert(entities);
}
