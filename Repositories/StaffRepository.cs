using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class StaffRepository : IStaffRepository
{
    public List<Staff> GetAll() => StaffDAO.GetAll();
    public Staff? GetById(int id) => StaffDAO.GetById(id);
    public void Save(Staff entity) => StaffDAO.Save(entity);
    public void Update(Staff entity) => StaffDAO.Update(entity);
    public void Delete(int id) => StaffDAO.Delete(id);
}


