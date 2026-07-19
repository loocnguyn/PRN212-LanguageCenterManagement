using BusinessObjects;

namespace Services;

// ICourseService — service contract for Course operations.

public interface ICourseService
{
    List<Course> GetAll();
    Course? GetById(int id);
    void Save(Course entity);
    void Update(Course entity);
    void Delete(int id);
}


