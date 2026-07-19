using BusinessObjects;

namespace Services;

// ISemesterService — service contract for Semester operations.

public interface ISemesterService
{
    List<Semester> GetAll();
    Semester? GetById(int id);
    Semester? GetActive();
    void Save(Semester semester);
    void Update(Semester semester);
    void Delete(int id);
    void SetActive(int semesterId);
    Phase GetPhase(Semester semester);
    Phase? GetActivePhase();
}
