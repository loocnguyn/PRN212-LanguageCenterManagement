using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class TuitionDiscountRepository : ITuitionDiscountRepository
{
    public List<TuitionDiscount> GetAll() => TuitionDiscountDAO.GetAll();
    public List<TuitionDiscount> GetActive(DateOnly date) => TuitionDiscountDAO.GetActive(date);
    public List<TuitionDiscount> Search(string? keyword, string? status) => TuitionDiscountDAO.Search(keyword, status);
    public TuitionDiscount? GetById(int id) => TuitionDiscountDAO.GetById(id);
    public void Save(TuitionDiscount entity) => TuitionDiscountDAO.Save(entity);
    public void Update(TuitionDiscount entity) => TuitionDiscountDAO.Update(entity);
    public void DeleteOrDeactivate(int id) => TuitionDiscountDAO.DeleteOrDeactivate(id);
    public bool IsCodeTaken(string code, int? excludeId = null) => TuitionDiscountDAO.IsCodeTaken(code, excludeId);
}
