using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  ClassScheduleService — weekly timetable slots for classes.
//  CONTENTS:
//    1. CRUD pass-through   — delegates to the repository
//    2. CheckConflicts      — teacher/room double-booking check
// ============================================================
public class ClassScheduleService : IClassScheduleService
{
    private readonly IClassScheduleRepository _repo = new ClassScheduleRepository();

    // ---- 1. CRUD pass-through ----------------------------------
    public List<ClassSchedule> GetAll() => _repo.GetAll();
    public ClassSchedule? GetById(int id) => _repo.GetById(id);
    public void Save(ClassSchedule entity) => _repo.Save(entity);
    public void Update(ClassSchedule entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);

    // ---- 2. Conflict detection ---------------------------------
    /// <summary>Returns a human-readable list of scheduling conflicts for <paramref name="schedule"/>:
    /// another class on the same day at an overlapping time that shares this class's teacher or room.
    /// Empty list means no conflict. The schedule being edited is excluded (matched by ScheduleId).</summary>
    public List<string> CheckConflicts(ClassSchedule schedule)
    {
        var conflicts = new List<string>();
        var allSchedules = _repo.GetAll();
        var classService = new ClassService();

        // Two time ranges overlap iff each starts before the other ends.
        // (start < other.End && End > other.start). Same-day only; skip self on edit.
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


