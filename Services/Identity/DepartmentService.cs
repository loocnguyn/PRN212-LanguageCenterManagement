using BusinessObjects;
using Repositories;

namespace Services;

// DepartmentService — business-layer entry point for departments (pass-through to the repository).
public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo = new DepartmentRepository();

    public List<Department> GetAll() => _repo.GetAll();
    public Department? GetById(int id) => _repo.GetById(id);
    public void Save(Department entity) => _repo.Save(entity);
    public void Update(Department entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}
