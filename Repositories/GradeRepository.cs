using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class GradeRepository : IGradeRepository
{
    public List<Grade> GetAll() => GradeDAO.GetAll();
    public Grade? GetById(int id) => GradeDAO.GetById(id);
    public void Save(Grade entity) => GradeDAO.Save(entity);
    public void Update(Grade entity) => GradeDAO.Update(entity);
    public void Delete(int id) => GradeDAO.Delete(id);
}


