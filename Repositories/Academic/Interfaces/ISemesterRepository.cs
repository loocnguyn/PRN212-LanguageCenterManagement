using BusinessObjects;

namespace Repositories;

// ISemesterRepository — repository contract for Semester persistence.

public interface ISemesterRepository
{
    List<Semester> GetAll();
    Semester? GetById(int id);
    Semester? GetActive();
    void Save(Semester semester);
    void Update(Semester semester);
    void Delete(int id);
    void SetActive(int semesterId);
}
