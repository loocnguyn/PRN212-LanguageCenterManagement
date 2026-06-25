using BusinessObjects;

namespace Repositories;

public interface IStaffRepository
{
    List<Staff> GetAll();
    Staff? GetById(int id);
    void Save(Staff entity);
    void Update(Staff entity);
    void Delete(int id);
}


