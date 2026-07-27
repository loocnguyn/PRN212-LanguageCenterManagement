using BusinessObjects;

namespace Services;

// IStudentService — service contract for Student operations.

public interface IStudentService
{
    List<Student> GetAll();
    Student? GetById(int id);

    /// <summary>
    /// The student profile behind a signed-in account. Prefer this over filtering
    /// GetAll() in the UI: it asks the database for one row instead of all of them.
    /// </summary>
    Student? GetByUserId(int userId);
    void Save(Student entity);
    void Update(Student entity);
    void Delete(int id);
}


