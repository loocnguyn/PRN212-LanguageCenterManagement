using BusinessObjects;

namespace Repositories;

public interface IStudentRepository
{
    List<Student> GetAll();
    Student? GetById(int id);
    void Save(Student entity);
    void Update(Student entity);
    void Delete(int id);
}


