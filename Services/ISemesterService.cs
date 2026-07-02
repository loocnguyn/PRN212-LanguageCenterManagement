using BusinessObjects;

namespace Services;

public interface ISemesterService
{
    List<Semester> GetAll();
    Semester? GetById(int id);
    Semester? GetActive();
    void Save(Semester semester);
    void Update(Semester semester);
    void Delete(int id);
}
