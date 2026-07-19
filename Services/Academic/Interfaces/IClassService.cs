using BusinessObjects;

namespace Services;

// IClassService — service contract for Class operations.

public interface IClassService
{
    List<Class> GetAll();
    Class? GetById(int id);
    void Save(Class entity);
    void Update(Class entity);
    void Delete(int id);
    List<Class> GetBySemesterId(int semesterId);
    List<Class> GetClassesWithDetails(int semesterId);
}
