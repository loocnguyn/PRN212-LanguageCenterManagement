using BusinessObjects;
using Repositories;

namespace Services;

public class ClassScheduleService : IClassScheduleService
{
    private readonly IClassScheduleRepository _repo = new ClassScheduleRepository();

    public List<ClassSchedule> GetAll() => _repo.GetAll();
    public ClassSchedule? GetById(int id) => _repo.GetById(id);
    public void Save(ClassSchedule entity) => _repo.Save(entity);
    public void Update(ClassSchedule entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);

    public List<string> CheckConflicts(ClassSchedule schedule)
    {
        var conflicts = new List<string>();
        var allSchedules = _repo.GetAll();
        var classService = new ClassService();

        // Find schedules on same day with overlapping time (excluding self on edit)
        var overlapping = allSchedules
            .Where(s => s.ScheduleId != schedule.ScheduleId
                        && s.DayOfWeek == schedule.DayOfWeek
                        && s.StartTime < schedule.EndTime
                        && s.EndTime > schedule.StartTime)
            .ToList();

        var currentClass = classService.GetById(schedule.ClassId);
        if (currentClass == null) return conflicts;

        foreach (var other in overlapping)
        {
            var otherClass = classService.GetById(other.ClassId);
            if (otherClass == null) continue;

            if (currentClass.TeacherId == otherClass.TeacherId)
                conflicts.Add($"Teacher conflict with class '{otherClass.Name}' on same day and overlapping time.");
            if (currentClass.ClassroomId == otherClass.ClassroomId)
                conflicts.Add($"Room conflict with class '{otherClass.Name}' on same day and overlapping time.");
        }

        return conflicts;
    }
}


