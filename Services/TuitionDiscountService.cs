using BusinessObjects;
using Repositories;

namespace Services;

public class TuitionDiscountService : ITuitionDiscountService
{
    private readonly ITuitionDiscountRepository _repo;

    public TuitionDiscountService() : this(new TuitionDiscountRepository())
    {
    }

    public TuitionDiscountService(ITuitionDiscountRepository repo)
    {
        _repo = repo;
    }

    public List<TuitionDiscount> GetAll() => _repo.GetAll();
    public List<TuitionDiscount> GetActive(DateOnly date) => _repo.GetActive(date);
    public List<TuitionDiscount> Search(string? keyword, string? status) => _repo.Search(keyword, status);
    public TuitionDiscount? GetById(int id) => _repo.GetById(id);

    public void Save(TuitionDiscount entity)
    {
        NormalizeAndValidate(entity);
        if (_repo.IsCodeTaken(entity.Code))
            throw new InvalidOperationException($"Discount code '{entity.Code}' already exists.");

        entity.CreatedAt = DateTime.Now;
        _repo.Save(entity);
    }

    public void Update(TuitionDiscount entity)
    {
        NormalizeAndValidate(entity);
        if (_repo.IsCodeTaken(entity.Code, entity.DiscountId))
            throw new InvalidOperationException($"Discount code '{entity.Code}' already exists.");

        _repo.Update(entity);
    }

    public void DeleteOrDeactivate(int id) => _repo.DeleteOrDeactivate(id);

    private static void NormalizeAndValidate(TuitionDiscount entity)
    {
        entity.Code = entity.Code.Trim().ToUpperInvariant();
        entity.Name = entity.Name.Trim();
        entity.DiscountType = entity.DiscountType.Trim().ToUpperInvariant();
        entity.ConditionType = entity.ConditionType.Trim().ToUpperInvariant();
        entity.Note = string.IsNullOrWhiteSpace(entity.Note) ? null : entity.Note.Trim();

        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new InvalidOperationException("Discount code is required.");
        if (entity.Code.Length > 50)
            throw new InvalidOperationException("Discount code must be 50 characters or less.");
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new InvalidOperationException("Discount name is required.");
        if (entity.Name.Length > 150)
            throw new InvalidOperationException("Discount name must be 150 characters or less.");
        if (entity.DiscountType is not ("PERCENT" or "FIXED"))
            throw new InvalidOperationException("Discount type must be PERCENT or FIXED.");
        if (entity.DiscountValue <= 0)
            throw new InvalidOperationException("Discount value must be greater than 0.");
        if (entity.DiscountType == "PERCENT" && entity.DiscountValue > 100)
            throw new InvalidOperationException("Percent discount cannot be greater than 100.");
        if (entity.StartDate.HasValue && entity.EndDate.HasValue && entity.EndDate < entity.StartDate)
            throw new InvalidOperationException("End date must be greater than or equal to start date.");
        if (entity.ConditionType is not ("NONE" or "EARLY_PAYMENT"))
            throw new InvalidOperationException("Condition type must be NONE or EARLY_PAYMENT.");
        if (entity.ConditionType == "EARLY_PAYMENT")
        {
            if (!entity.PaymentDeadlineDays.HasValue || entity.PaymentDeadlineDays <= 0)
                throw new InvalidOperationException("Early payment discount requires payment deadline days greater than 0.");
        }
        else
        {
            entity.PaymentDeadlineDays = null;
        }
    }
}
