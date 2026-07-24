using BusinessObjects;

namespace Services;

// ISemesterService — service contract for Semester operations.

public interface ISemesterService
{
    List<Semester> GetAll();
    Semester? GetById(int id);

    /// <summary>The semester containing today, or null when today falls between semesters.</summary>
    Semester? GetActive();

    /// <summary>Validates then inserts. Throws InvalidOperationException with a user-facing message.</summary>
    void Save(Semester semester);

    /// <summary>
    /// Validates then updates. Throws InvalidOperationException with a user-facing message —
    /// including when the semester has already left SETUP (see <see cref="IsEditable"/>).
    /// </summary>
    void Update(Semester semester);

    /// <summary>Throws InvalidOperationException if the semester still has classes.</summary>
    void Delete(int id);

    Phase GetPhase(Semester semester);
    Phase? GetActivePhase();

    /// <summary>True while the semester's details may still be changed — i.e. it is still in SETUP.</summary>
    bool IsEditable(Semester semester);
}
