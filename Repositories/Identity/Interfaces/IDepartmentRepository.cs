using BusinessObjects;

namespace Repositories;

// IDepartmentRepository — repository contract for Department persistence.
public interface IDepartmentRepository
{
    List<Department> GetAll();
    Department? GetById(int id);
    void Save(Department entity);
    void Update(Department entity);
    void Delete(int id);
}
