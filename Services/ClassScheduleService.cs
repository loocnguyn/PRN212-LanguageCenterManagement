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
}


