using BusinessObjects;

namespace Services;

// IGradeTypeService — service contract for GradeType operations.

public interface IGradeTypeService
{
    List<GradeType> GetAll();
    GradeType? GetById(int id);
    List<GradeType> GetByCourseId(int courseId);
    void Save(GradeType entity);
    void Update(GradeType entity);
    void Delete(int id);

    /// <summary>Sum of WeightPercent across a course's grade types, optionally excluding one
    /// (e.g. the row being edited) — use to validate the total equals 100 before saving.</summary>
    decimal GetTotalWeightPercent(int courseId, int? excludeGradeTypeId = null);
}


