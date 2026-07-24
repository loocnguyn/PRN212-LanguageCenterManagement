using BusinessObjects;
using Repositories;

namespace Services;

// PaymentService — business-logic entry point for Payment (mostly delegates to the repository).

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo = new PaymentRepository();

    public List<Payment> GetAll() => _repo.GetAll();
    public Payment? GetById(int id) => _repo.GetById(id);
    public void Save(Payment entity) => _repo.Save(entity);
    public void Update(Payment entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
    public void RecordPayment(Payment payment) => _repo.RecordPayment(payment);
    public List<Payment> GetPaymentsByDateRange(DateTime? fromDate, DateTime? toDate, string? method)
        => _repo.GetPaymentsByDateRange(fromDate, toDate, method);
}
