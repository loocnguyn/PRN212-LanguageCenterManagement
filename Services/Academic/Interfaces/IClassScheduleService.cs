using BusinessObjects;

namespace Services;

// IClassScheduleService — service contract for ClassSchedule operations.

public interface IClassScheduleService
{
    List<ClassSchedule> GetAll();
    ClassSchedule? GetById(int id);
    void Save(ClassSchedule entity);
    void Update(ClassSchedule entity);
    void Delete(int id);
    List<string> CheckConflicts(ClassSchedule schedule);
}


