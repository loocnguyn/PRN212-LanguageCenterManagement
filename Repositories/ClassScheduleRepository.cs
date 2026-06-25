using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class ClassScheduleRepository : IClassScheduleRepository
{
    public List<ClassSchedule> GetAll() => ClassScheduleDAO.GetAll();
    public ClassSchedule? GetById(int id) => ClassScheduleDAO.GetById(id);
    public void Save(ClassSchedule entity) => ClassScheduleDAO.Save(entity);
    public void Update(ClassSchedule entity) => ClassScheduleDAO.Update(entity);
    public void Delete(int id) => ClassScheduleDAO.Delete(id);
}


