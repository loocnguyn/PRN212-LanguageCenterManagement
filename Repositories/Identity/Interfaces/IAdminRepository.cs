using BusinessObjects;

namespace Repositories;

// IAdminRepository — repository contract for Admin persistence.

public interface IAdminRepository
{
    List<Admin> GetAll();
    Admin? GetById(int id);

    /// <summary>The admin profile belonging to a user account, or null.</summary>
    Admin? GetByUserId(int userId);
    void Save(Admin entity);
    void Update(Admin entity);
    void Delete(int id);
}


