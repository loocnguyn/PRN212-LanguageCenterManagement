using BusinessObjects;

namespace Repositories;

// IStaffRepository — repository contract for Staff persistence.

public interface IStaffRepository
{
    List<Staff> GetAll();
    Staff? GetById(int id);
    void Save(Staff entity);
    void Update(Staff entity);
    void Delete(int id);
}


