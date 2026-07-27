using BusinessObjects;

namespace Repositories;

// IGradeRepository — repository contract for Grade persistence.

public interface IGradeRepository
{
    List<Grade> GetAll();
    Grade? GetById(int id);
    void Save(Grade entity);
    void Update(Grade entity);
    void Delete(int id);
    List<Grade> GetByEnrollmentId(int enrollmentId);
    List<Grade> GetByEnrollmentIds(List<int> enrollmentIds);
    void Upsert(Grade entity);
    List<Grade> GetByStudentId(int studentId);
    void BulkUpsert(List<Grade> entities);
}
