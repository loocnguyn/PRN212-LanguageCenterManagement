using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class ClassroomRepository : IClassroomRepository
{
    public List<Classroom> GetAll() => ClassroomDAO.GetAll();
    public Classroom? GetById(int id) => ClassroomDAO.GetById(id);
    public void Save(Classroom entity) => ClassroomDAO.Save(entity);
    public void Update(Classroom entity) => ClassroomDAO.Update(entity);
    public void Delete(int id) => ClassroomDAO.Delete(id);
}


