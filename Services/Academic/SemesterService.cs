using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  SemesterService — semester rules live here, not in the window.
//  CONTENTS:
//    1. Reads            — GetAll / GetById / GetActive
//    2. Save / Update    — validated writes (dates, name, overlap)
//    3. Delete           — blocked while the semester still has classes
//    4. Phase            — SETUP / LEARNING / COMPLETED from the dates
//
//  Two invariants this class exists to protect:
//    * Semesters never overlap. "Which semester is now?" must have exactly one
//      answer, because activeness is derived from today's date rather than a
//      stored flag (see Semester.IsActive).
//    * SetupEndDate sits inside [StartDate, EndDate]. Session generation starts
//      the day after it (SessionService), so a value outside the semester would
//      silently produce a class with no sessions.
// ============================================================

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _repo = new SemesterRepository();

    // ---- 1. Reads ----------------------------------------------
    public List<Semester> GetAll() => _repo.GetAll();
    public Semester? GetById(int id) => _repo.GetById(id);

    /// <summary>The semester containing today, or null when today falls in a gap between semesters.</summary>
    public Semester? GetActive() => _repo.GetActive();

    // ---- 2. Validated writes -----------------------------------
    public void Save(Semester semester)
    {
        Validate(semester, excludeId: null);
        _repo.Save(semester);
    }

    public void Update(Semester semester)
    {
        Validate(semester, excludeId: semester.SemesterId);
        _repo.Update(semester);
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> with a user-facing message if the
    /// semester is not saveable. <paramref name="excludeId"/> is the row being edited, so
    /// it is not compared against itself.
    /// </summary>
    private void Validate(Semester semester, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(semester.Name))
            throw new InvalidOperationException("Semester name is required.");

        semester.Name = semester.Name.Trim();

        if (semester.EndDate <= semester.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        if (semester.SetupEndDate < semester.StartDate || semester.SetupEndDate >= semester.EndDate)
            throw new InvalidOperationException(
                "Setup end date must fall on or after the start date, and before the end date — " +
                "teaching starts the day after it.");

        if (_repo.NameExists(semester.Name, excludeId))
            throw new InvalidOperationException($"A semester named \"{semester.Name}\" already exists.");

        var clashes = _repo.GetOverlapping(semester.StartDate, semester.EndDate, excludeId);
        if (clashes.Count > 0)
        {
            var detail = string.Join("\n", clashes.Select(c =>
                $"  • {c.Name}  ({c.StartDate:dd/MM/yyyy} – {c.EndDate:dd/MM/yyyy})"));
            throw new InvalidOperationException(
                $"These dates overlap {clashes.Count} existing semester(s):\n{detail}\n\n" +
                "Semesters cannot overlap — the system decides the current semester from today's date.");
        }
    }

    // ---- 3. Delete ---------------------------------------------
    public void Delete(int id)
    {
        var classCount = _repo.CountClasses(id);
        if (classCount > 0)
            throw new InvalidOperationException(
                $"Cannot delete this semester — it still has {classCount} class(es).\n" +
                "Remove those classes (and their enrollments) first.");

        _repo.Delete(id);
    }

    // ---- 4. Phase ----------------------------------------------
    /// <summary>
    /// SETUP up to and including SetupEndDate, LEARNING from the next day through EndDate,
    /// COMPLETED after that. The SETUP boundary is inclusive so it lines up with
    /// SessionService, which generates the first session on SetupEndDate + 1.
    /// </summary>
    public Phase GetPhase(Semester semester)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (today <= semester.SetupEndDate) return Phase.SETUP;
        if (today <= semester.EndDate) return Phase.LEARNING;
        return Phase.COMPLETED;
    }

    public Phase? GetActivePhase()
    {
        var active = GetActive();
        return active == null ? null : GetPhase(active);
    }
}
