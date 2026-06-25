using BusinessObjects;
using Repositories;

namespace Services;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _repo = new StaffRepository();

    public List<Staff> GetAll() => _repo.GetAll();
    public Staff? GetById(int id) => _repo.GetById(id);
    public void Save(Staff entity) => _repo.Save(entity);
    public void Update(Staff entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


