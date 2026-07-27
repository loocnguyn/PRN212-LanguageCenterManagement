using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// PaymentRepository — thin pass-through from the service layer to PaymentDAO.

public class PaymentRepository : IPaymentRepository
{
    public List<Payment> GetAll() => PaymentDAO.GetAll();
    public Payment? GetById(int id) => PaymentDAO.GetById(id);
    public void Save(Payment entity) => PaymentDAO.Save(entity);
    public void Update(Payment entity) => PaymentDAO.Update(entity);
    public void Delete(int id) => PaymentDAO.Delete(id);
    public void RecordPayment(Payment payment) => PaymentDAO.RecordPayment(payment);
    public List<Payment> GetPaymentsByDateRange(DateTime? fromDate, DateTime? toDate, string? method)
        => PaymentDAO.GetPaymentsByDateRange(fromDate, toDate, method);
}
