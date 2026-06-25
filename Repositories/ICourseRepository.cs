using BusinessObjects;

namespace Repositories;

public interface ICourseRepository
{
    List<Course> GetAll();
    Course? GetById(int id);
    void Save(Course entity);
    void Update(Course entity);
    void Delete(int id);
}


