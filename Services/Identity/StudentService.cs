using BusinessObjects;
using Repositories;

namespace Services;

// StudentService — business-logic entry point for Student (mostly delegates to the repository).

public class StudentService : IStudentService
{
    private readonly IStudentRepository _repo = new StudentRepository();

    public List<Student> GetAll() => _repo.GetAll();
    public Student? GetById(int id) => _repo.GetById(id);
    public Student? GetByUserId(int userId) => _repo.GetByUserId(userId);
    public void Save(Student entity) => _repo.Save(entity);
    public void Update(Student entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


