using BusinessObjects;

namespace Repositories;

// IStudentRepository — repository contract for Student persistence.

public interface IStudentRepository
{
    List<Student> GetAll();
    Student? GetById(int id);

    /// <summary>The student profile belonging to a user account, or null.</summary>
    Student? GetByUserId(int userId);
    void Save(Student entity);
    void Update(Student entity);
    void Delete(int id);
}


