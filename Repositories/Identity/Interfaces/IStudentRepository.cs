using BusinessObjects;

namespace Repositories;

// IStudentRepository — repository contract for Student persistence.

public interface IStudentRepository
{
    List<Student> GetAll();
    Student? GetById(int id);
    void Save(Student entity);
    void Update(Student entity);
    void Delete(int id);
}


