using BusinessObjects;

namespace Services;

// IStudentService — service contract for Student operations.

public interface IStudentService
{
    List<Student> GetAll();
    Student? GetById(int id);
    void Save(Student entity);
    void Update(Student entity);
    void Delete(int id);
}


