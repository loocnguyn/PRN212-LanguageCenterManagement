using BusinessObjects;

namespace Repositories;

public interface IClassScheduleRepository
{
    List<ClassSchedule> GetAll();
    ClassSchedule? GetById(int id);
    void Save(ClassSchedule entity);
    void Update(ClassSchedule entity);
    void Delete(int id);
}


