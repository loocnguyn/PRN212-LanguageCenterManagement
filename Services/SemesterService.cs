using BusinessObjects;
using Repositories;

namespace Services;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _repo = new SemesterRepository();

    public List<Semester> GetAll() => _repo.GetAll();
    public Semester? GetById(int id) => _repo.GetById(id);
    public Semester? GetActive() => _repo.GetActive();
    public void Save(Semester semester) => _repo.Save(semester);
    public void Update(Semester semester) => _repo.Update(semester);
    public void Delete(int id) => _repo.Delete(id);

    public void SetActive(int semesterId) => _repo.SetActive(semesterId);

    public Phase GetPhase(Semester semester)
    {
        if (semester.SetupEndDate == null)
            throw new InvalidOperationException($"Semester '{semester.Name}' has no SetupEndDate.");

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (today < semester.SetupEndDate.Value)
            return Phase.SETUP;
        if (today <= semester.EndDate)
            return Phase.LEARNING;
        return Phase.COMPLETED;
    }

    public Phase? GetActivePhase()
    {
        var active = GetActive();
        return active == null ? null : GetPhase(active);
    }
}
