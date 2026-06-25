using BusinessObjects;

namespace Services;

public interface IGradeTypeService
{
    List<GradeType> GetAll();
    GradeType? GetById(int id);
    void Save(GradeType entity);
    void Update(GradeType entity);
    void Delete(int id);
}


