using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// StudentRepository — thin pass-through from the service layer to StudentDAO.

public class StudentRepository : IStudentRepository
{
    public List<Student> GetAll() => StudentDAO.GetAll();
    public Student? GetById(int id) => StudentDAO.GetById(id);
    public void Save(Student entity) => StudentDAO.Save(entity);
    public void Update(Student entity) => StudentDAO.Update(entity);
    public void Delete(int id) => StudentDAO.Delete(id);
}


