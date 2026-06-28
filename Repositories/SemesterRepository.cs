using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class SemesterRepository : ISemesterRepository
{
    public List<Semester> GetAll() => SemesterDAO.GetAll();
    public Semester? GetById(int id) => SemesterDAO.GetById(id);
    public Semester? GetActive() => SemesterDAO.GetActive();
    public void Save(Semester semester) => SemesterDAO.Save(semester);
    public void Update(Semester semester) => SemesterDAO.Update(semester);
    public void Delete(int id) => SemesterDAO.Delete(id);
}
