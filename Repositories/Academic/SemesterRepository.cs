using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// SemesterRepository — thin pass-through from the service layer to SemesterDAO.

public class SemesterRepository : ISemesterRepository
{
    public List<Semester> GetAll() => SemesterDAO.GetAll();
    public Semester? GetById(int id) => SemesterDAO.GetById(id);
    public Semester? GetActive() => SemesterDAO.GetActive();

    public List<Semester> GetOverlapping(DateOnly start, DateOnly end, int? excludeId = null)
        => SemesterDAO.GetOverlapping(start, end, excludeId);

    public bool NameExists(string name, int? excludeId = null) => SemesterDAO.NameExists(name, excludeId);
    public int CountClasses(int semesterId) => SemesterDAO.CountClasses(semesterId);

    public void Save(Semester semester) => SemesterDAO.Save(semester);
    public void Update(Semester semester) => SemesterDAO.Update(semester);
    public void Delete(int id) => SemesterDAO.Delete(id);
}
