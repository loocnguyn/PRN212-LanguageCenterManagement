using BusinessObjects;

namespace Repositories;

public interface IAdminRepository
{
    List<Admin> GetAll();
    Admin? GetById(int id);
    void Save(Admin entity);
    void Update(Admin entity);
    void Delete(int id);
}


