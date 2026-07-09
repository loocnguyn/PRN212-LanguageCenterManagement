using System.Linq;
using BusinessObjects;
using Repositories;

namespace Services;

public class GradeTypeService : IGradeTypeService
{
    private readonly IGradeTypeRepository _repo = new GradeTypeRepository();

    public List<GradeType> GetAll() => _repo.GetAll();
    public GradeType? GetById(int id) => _repo.GetById(id);
    public List<GradeType> GetByCourseId(int courseId) => _repo.GetByCourseId(courseId);
    public void Save(GradeType entity) => _repo.Save(entity);
    public void Update(GradeType entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);

    public decimal GetTotalWeightPercent(int courseId, int? excludeGradeTypeId = null)
        => _repo.GetByCourseId(courseId)
            .Where(gt => gt.GradeTypeId != excludeGradeTypeId)
            .Sum(gt => gt.WeightPercent);
}


