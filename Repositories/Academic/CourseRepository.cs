using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// CourseRepository — thin pass-through from the service layer to CourseDAO.

public class CourseRepository : ICourseRepository
{
    public List<Course> GetAll() => CourseDAO.GetAll();
    public Course? GetById(int id) => CourseDAO.GetById(id);
    public void Save(Course entity) => CourseDAO.Save(entity);
    public void Update(Course entity) => CourseDAO.Update(entity);
    public void Delete(int id) => CourseDAO.Delete(id);
}


