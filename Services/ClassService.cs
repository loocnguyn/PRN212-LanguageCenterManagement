using BusinessObjects;
using Repositories;

namespace Services;

public class ClassService : IClassService
{
    private readonly IClassRepository _repo = new ClassRepository();

    public List<Class> GetAll() => _repo.GetAll();
    public Class? GetById(int id) => _repo.GetById(id);
    public void Save(Class entity) => _repo.Save(entity);
    public void Update(Class entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


