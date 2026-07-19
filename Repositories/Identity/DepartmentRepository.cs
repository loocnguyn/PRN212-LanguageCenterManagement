using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// DepartmentRepository — thin pass-through from the service layer to DepartmentDAO.
public class DepartmentRepository : IDepartmentRepository
{
    public List<Department> GetAll() => DepartmentDAO.GetAll();
    public Department? GetById(int id) => DepartmentDAO.GetById(id);
    public void Save(Department entity) => DepartmentDAO.Save(entity);
    public void Update(Department entity) => DepartmentDAO.Update(entity);
    public void Delete(int id) => DepartmentDAO.Delete(id);
}
