using BusinessObjects;

namespace Services;

// IClassroomService — service contract for Classroom operations.

public interface IClassroomService
{
    List<Classroom> GetAll();
    Classroom? GetById(int id);
    void Save(Classroom entity);
    void Update(Classroom entity);
    void Delete(int id);
}


