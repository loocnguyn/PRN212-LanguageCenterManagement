using BusinessObjects;

namespace Repositories;

public interface IPaymentRepository
{
    List<Payment> GetAll();
    Payment? GetById(int id);
    void Save(Payment entity);
    void Update(Payment entity);
    void Delete(int id);
}


