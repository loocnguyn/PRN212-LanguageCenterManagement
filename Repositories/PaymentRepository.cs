using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class PaymentRepository : IPaymentRepository
{
    public List<Payment> GetAll() => PaymentDAO.GetAll();
    public Payment? GetById(int id) => PaymentDAO.GetById(id);
    public void Save(Payment entity) => PaymentDAO.Save(entity);
    public void Update(Payment entity) => PaymentDAO.Update(entity);
    public void Delete(int id) => PaymentDAO.Delete(id);
    public void RecordPayment(Payment payment) => PaymentDAO.RecordPayment(payment);
}
