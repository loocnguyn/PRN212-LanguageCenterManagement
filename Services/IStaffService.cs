using BusinessObjects;

namespace Services;

public interface IStaffService
{
    List<Staff> GetAll();
    Staff? GetById(int id);
    void Save(Staff entity);
    void Update(Staff entity);
    void Delete(int id);
}


