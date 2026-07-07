using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class ClassRepository : IClassRepository
{
    public List<Class> GetAll() => ClassDAO.GetAll();
    public Class? GetById(int id) => ClassDAO.GetById(id);
    public void Save(Class entity) => ClassDAO.Save(entity);
    public void Update(Class entity) => ClassDAO.Update(entity);
    public void Delete(int id) => ClassDAO.Delete(id);
    public List<Class> GetBySemesterId(int semesterId) => ClassDAO.GetBySemesterId(semesterId);
    public void UpdateStatus(int classId, string status) => ClassDAO.UpdateStatus(classId, status);
    public List<Class> GetBySemesterIdWithDetails(int semesterId) => ClassDAO.GetBySemesterIdWithDetails(semesterId);
}
