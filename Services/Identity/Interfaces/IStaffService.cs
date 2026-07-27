using BusinessObjects;

namespace Services;

// IStaffService — service contract for Staff operations.

public interface IStaffService
{
    List<Staff> GetAll();
    Staff? GetById(int id);

    /// <summary>
    /// The staff profile behind a signed-in account. Prefer this over filtering
    /// GetAll() in the UI: it asks the database for one row instead of all of them.
    /// </summary>
    Staff? GetByUserId(int userId);
    void Save(Staff entity);
    void Update(Staff entity);
    void Delete(int id);
}


