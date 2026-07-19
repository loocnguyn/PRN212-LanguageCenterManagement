using BusinessObjects;
using Repositories;

namespace Services;

// ClassroomService — business-logic entry point for Classroom (mostly delegates to the repository).

public class ClassroomService : IClassroomService
{
    private readonly IClassroomRepository _repo = new ClassroomRepository();

    public List<Classroom> GetAll() => _repo.GetAll();
    public Classroom? GetById(int id) => _repo.GetById(id);
    public void Save(Classroom entity) => _repo.Save(entity);
    public void Update(Classroom entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


