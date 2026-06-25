using BusinessObjects;

namespace Services;

public interface IClassroomService
{
    List<Classroom> GetAll();
    Classroom? GetById(int id);
    void Save(Classroom entity);
    void Update(Classroom entity);
    void Delete(int id);
}


