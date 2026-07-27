using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// AdminRepository — thin pass-through from the service layer to AdminDAO.

public class AdminRepository : IAdminRepository
{
    public List<Admin> GetAll() => AdminDAO.GetAll();
    public Admin? GetById(int id) => AdminDAO.GetById(id);

    public Admin? GetByUserId(int userId) => AdminDAO.GetByUserId(userId);
    public void Save(Admin entity) => AdminDAO.Save(entity);
    public void Update(Admin entity) => AdminDAO.Update(entity);
    public void Delete(int id) => AdminDAO.Delete(id);
}


