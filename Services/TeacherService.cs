using BusinessObjects;
using Repositories;

namespace Services;

public class TeacherService : ITeacherService
{
    private readonly ITeacherRepository _repo = new TeacherRepository();

    public List<Teacher> GetAll() => _repo.GetAll();
    public Teacher? GetById(int id) => _repo.GetById(id);
    public void Save(Teacher entity) => _repo.Save(entity);
    public void Update(Teacher entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


