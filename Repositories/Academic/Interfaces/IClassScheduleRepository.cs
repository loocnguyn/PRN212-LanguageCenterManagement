using BusinessObjects;

namespace Repositories;

// IClassScheduleRepository — repository contract for ClassSchedule persistence.

public interface IClassScheduleRepository
{
    List<ClassSchedule> GetAll();
    ClassSchedule? GetById(int id);
    void Save(ClassSchedule entity);
    void Update(ClassSchedule entity);
    void Delete(int id);
    List<ClassSchedule> GetByClassId(int classId);
}


