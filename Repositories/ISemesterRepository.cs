using BusinessObjects;

namespace Repositories;

public interface ISemesterRepository
{
    List<Semester> GetAll();
    Semester? GetById(int id);
    Semester? GetActive();
    void Save(Semester semester);
    void Update(Semester semester);
    void Delete(int id);
}
