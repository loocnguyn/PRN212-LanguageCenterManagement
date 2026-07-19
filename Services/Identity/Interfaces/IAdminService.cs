using BusinessObjects;

namespace Services;

// IAdminService — service contract for Admin operations.

public interface IAdminService
{
    List<Admin> GetAll();
    Admin? GetById(int id);
    void Save(Admin entity);
    void Update(Admin entity);
    void Delete(int id);
}


