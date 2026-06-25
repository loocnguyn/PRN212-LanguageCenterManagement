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
}


