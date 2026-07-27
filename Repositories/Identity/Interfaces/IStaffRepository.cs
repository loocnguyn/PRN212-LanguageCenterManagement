using BusinessObjects;

namespace Repositories;

// IStaffRepository — repository contract for Staff persistence.

public interface IStaffRepository
{
    List<Staff> GetAll();
    Staff? GetById(int id);

    /// <summary>The staff profile belonging to a user account, or null.</summary>
    Staff? GetByUserId(int userId);
    void Save(Staff entity);
    void Update(Staff entity);
    void Delete(int id);
}


