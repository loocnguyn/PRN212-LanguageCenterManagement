using BusinessObjects;

namespace Services;

// IDepartmentService — service contract used by the WPF layer for department operations.
public interface IDepartmentService
{
    List<Department> GetAll();
    Department? GetById(int id);
    void Save(Department entity);
    void Update(Department entity);
    void Delete(int id);
}
