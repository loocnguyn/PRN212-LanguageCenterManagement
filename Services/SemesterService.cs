using BusinessObjects;
using Repositories;

namespace Services;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _repo = new SemesterRepository();

    public List<Semester> GetAll() => _repo.GetAll();
    public Semester? GetById(int id) => _repo.GetById(id);
    public Semester? GetActive() => _repo.GetActive();
    public void Save(Semester semester) => _repo.Save(semester);
    public void Update(Semester semester) => _repo.Update(semester);
    public void Delete(int id) => _repo.Delete(id);
}
