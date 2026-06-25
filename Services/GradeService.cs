using BusinessObjects;
using Repositories;

namespace Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _repo = new GradeRepository();

    public List<Grade> GetAll() => _repo.GetAll();
    public Grade? GetById(int id) => _repo.GetById(id);
    public void Save(Grade entity) => _repo.Save(entity);
    public void Update(Grade entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


