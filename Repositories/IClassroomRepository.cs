using BusinessObjects;

namespace Repositories;

public interface IClassroomRepository
{
    List<Classroom> GetAll();
    Classroom? GetById(int id);
    void Save(Classroom entity);
    void Update(Classroom entity);
    void Delete(int id);
}


