using BusinessObjects;

namespace Services;

// ITeacherService — service contract for Teacher operations.

public interface ITeacherService
{
    List<Teacher> GetAll();
    Teacher? GetById(int id);
    Teacher? GetByUserId(int userId);
    void Save(Teacher entity);
    void Update(Teacher entity);
    void Delete(int id);
}


