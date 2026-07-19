using BusinessObjects;

namespace Services;

// ITuitionDiscountService — service contract for TuitionDiscount operations.

public interface ITuitionDiscountService
{
    List<TuitionDiscount> GetAll();
    List<TuitionDiscount> GetActive(DateOnly date);
    List<TuitionDiscount> Search(string? keyword, string? status);
    TuitionDiscount? GetById(int id);
    void Save(TuitionDiscount entity);
    void Update(TuitionDiscount entity);
    void DeleteOrDeactivate(int id);
}
