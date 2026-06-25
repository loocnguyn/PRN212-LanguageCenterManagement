using BusinessObjects;

namespace Services;

public interface IGradeService
{
    List<Grade> GetAll();
    Grade? GetById(int id);
    void Save(Grade entity);
    void Update(Grade entity);
    void Delete(int id);
}


