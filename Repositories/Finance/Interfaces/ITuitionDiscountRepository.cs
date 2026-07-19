using BusinessObjects;

namespace Repositories;

// ITuitionDiscountRepository — repository contract for TuitionDiscount persistence.

public interface ITuitionDiscountRepository
{
    List<TuitionDiscount> GetAll();
    List<TuitionDiscount> GetActive(DateOnly date);
    List<TuitionDiscount> Search(string? keyword, string? status);
    TuitionDiscount? GetById(int id);
    void Save(TuitionDiscount entity);
    void Update(TuitionDiscount entity);
    void DeleteOrDeactivate(int id);
    bool IsCodeTaken(string code, int? excludeId = null);
}
