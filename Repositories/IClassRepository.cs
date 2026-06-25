using BusinessObjects;

namespace Repositories;

public interface IClassRepository
{
    List<Class> GetAll();
    Class? GetById(int id);
    void Save(Class entity);
    void Update(Class entity);
    void Delete(int id);
}


