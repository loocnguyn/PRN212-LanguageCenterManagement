using BusinessObjects;

namespace Services;

public interface IClassService
{
    List<Class> GetAll();
    Class? GetById(int id);
    void Save(Class entity);
    void Update(Class entity);
    void Delete(int id);
}


