using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// StaffRepository — thin pass-through from the service layer to StaffDAO.

public class StaffRepository : IStaffRepository
{
    public List<Staff> GetAll() => StaffDAO.GetAll();
    public Staff? GetById(int id) => StaffDAO.GetById(id);

    public Staff? GetByUserId(int userId) => StaffDAO.GetByUserId(userId);
    public void Save(Staff entity) => StaffDAO.Save(entity);
    public void Update(Staff entity) => StaffDAO.Update(entity);
    public void Delete(int id) => StaffDAO.Delete(id);
}


