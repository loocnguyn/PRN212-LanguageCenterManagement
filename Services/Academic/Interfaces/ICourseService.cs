using BusinessObjects;

namespace Services;

public interface ICourseService
{
    List<Course> GetAll();
    Course? GetById(int id);
    void Save(Course entity);
    void Update(Course entity);
    void Delete(int id);
}


