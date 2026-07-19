using BusinessObjects;

namespace Repositories;

// IClassRepository — repository contract for Class persistence.

public interface IClassRepository
{
    List<Class> GetAll();
    Class? GetById(int id);
    void Save(Class entity);
    void Update(Class entity);
    void Delete(int id);
    List<Class> GetBySemesterId(int semesterId);
    void UpdateStatus(int classId, string status);
    List<Class> GetBySemesterIdWithDetails(int semesterId);
}
