using BusinessObjects;

namespace Repositories;

// IGradeTypeRepository — repository contract for GradeType persistence.

public interface IGradeTypeRepository
{
    List<GradeType> GetAll();
    GradeType? GetById(int id);
    List<GradeType> GetByCourseId(int courseId);
    void Save(GradeType entity);
    void Update(GradeType entity);
    void Delete(int id);
}


