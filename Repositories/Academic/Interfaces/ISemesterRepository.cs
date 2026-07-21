using BusinessObjects;

namespace Repositories;

// ISemesterRepository — repository contract for Semester persistence.

public interface ISemesterRepository
{
    List<Semester> GetAll();
    Semester? GetById(int id);

    /// <summary>The semester containing today, or null if today falls between semesters.</summary>
    Semester? GetActive();

    /// <summary>Semesters clashing with [start, end], excluding <paramref name="excludeId"/>.</summary>
    List<Semester> GetOverlapping(DateOnly start, DateOnly end, int? excludeId = null);

    bool NameExists(string name, int? excludeId = null);
    int CountClasses(int semesterId);

    void Save(Semester semester);
    void Update(Semester semester);
    void Delete(int id);
}
