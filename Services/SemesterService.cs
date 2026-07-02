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

    public SemesterPhase GetPhase(Semester semester, DateOnly? on = null)
    {
        var today = on ?? DateOnly.FromDateTime(DateTime.Now);
        if (today < semester.StartDate) return SemesterPhase.Upcoming;
        if (today < semester.SetupEndDate) return SemesterPhase.Setup;
        if (today <= semester.EndDate) return SemesterPhase.Learning;
        return SemesterPhase.Closed;
    }

    public void SetActive(int semesterId)
    {
        foreach (var s in _repo.GetAll())
        {
            var shouldBeActive = s.SemesterId == semesterId;
            if (s.IsActive != shouldBeActive)
            {
                s.IsActive = shouldBeActive;
                _repo.Update(s);
            }
        }
    }
}
