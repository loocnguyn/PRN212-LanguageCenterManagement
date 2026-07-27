using BusinessObjects;

namespace Services;

// IAdminService — service contract for Admin operations.

public interface IAdminService
{
    List<Admin> GetAll();
    Admin? GetById(int id);

    /// <summary>
    /// The admin profile behind a signed-in account. Prefer this over filtering
    /// GetAll() in the UI: it asks the database for one row instead of all of them.
    /// </summary>
    Admin? GetByUserId(int userId);
    void Save(Admin entity);
    void Update(Admin entity);
    void Delete(int id);
}


