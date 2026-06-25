using BusinessObjects;

namespace Repositories;

public interface ITeacherRepository
{
    List<Teacher> GetAll();
    Teacher? GetById(int id);
    void Save(Teacher entity);
    void Update(Teacher entity);
    void Delete(int id);
}


