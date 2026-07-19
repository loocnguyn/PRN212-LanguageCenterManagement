using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// GradeTypeRepository — thin pass-through from the service layer to GradeTypeDAO.

public class GradeTypeRepository : IGradeTypeRepository
{
    public List<GradeType> GetAll() => GradeTypeDAO.GetAll();
    public GradeType? GetById(int id) => GradeTypeDAO.GetById(id);
    public List<GradeType> GetByCourseId(int courseId) => GradeTypeDAO.GetByCourseId(courseId);
    public void Save(GradeType entity) => GradeTypeDAO.Save(entity);
    public void Update(GradeType entity) => GradeTypeDAO.Update(entity);
    public void Delete(int id) => GradeTypeDAO.Delete(id);
}


