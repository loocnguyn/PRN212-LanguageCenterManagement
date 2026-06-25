using BusinessObjects;

namespace Services;

public interface IPaymentService
{
    List<Payment> GetAll();
    Payment? GetById(int id);
    void Save(Payment entity);
    void Update(Payment entity);
    void Delete(int id);
}


