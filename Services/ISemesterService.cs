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

    /// <summary>Returns the phase of the given semester for a given date (defaults to today).</summary>
    SemesterPhase GetPhase(Semester semester, DateOnly? on = null);

    /// <summary>Activates one semester and deactivates all others (only one active at a time).</summary>
    void SetActive(int semesterId);
}
