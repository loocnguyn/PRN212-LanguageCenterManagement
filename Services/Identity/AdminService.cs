using BusinessObjects;
using Repositories;

namespace Services;

// AdminService — business-logic entry point for Admin (mostly delegates to the repository).

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo = new AdminRepository();

    public List<Admin> GetAll() => _repo.GetAll();
    public Admin? GetById(int id) => _repo.GetById(id);
    public void Save(Admin entity) => _repo.Save(entity);
    public void Update(Admin entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


