using BusinessObjects;

namespace Repositories;

// IPaymentRepository — repository contract for Payment persistence.

public interface IPaymentRepository
{
    List<Payment> GetAll();
    Payment? GetById(int id);
    void Save(Payment entity);
    void Update(Payment entity);
    void Delete(int id);
    void RecordPayment(Payment payment);
    List<Payment> GetPaymentsByDateRange(DateTime fromDate, DateTime toDate, string? method);
}
