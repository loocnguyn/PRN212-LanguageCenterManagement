using BusinessObjects;
using Repositories;

namespace Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _repo = new CourseRepository();

    public List<Course> GetAll() => _repo.GetAll();
    public Course? GetById(int id) => _repo.GetById(id);
    public void Save(Course entity) => _repo.Save(entity);
    public void Update(Course entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


