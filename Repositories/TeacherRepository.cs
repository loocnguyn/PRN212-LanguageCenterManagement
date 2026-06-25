using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class TeacherRepository : ITeacherRepository
{
    public List<Teacher> GetAll() => TeacherDAO.GetAll();
    public Teacher? GetById(int id) => TeacherDAO.GetById(id);
    public void Save(Teacher entity) => TeacherDAO.Save(entity);
    public void Update(Teacher entity) => TeacherDAO.Update(entity);
    public void Delete(int id) => TeacherDAO.Delete(id);
}


