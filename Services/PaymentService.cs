using BusinessObjects;
using Repositories;

namespace Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo = new PaymentRepository();

    public List<Payment> GetAll() => _repo.GetAll();
    public Payment? GetById(int id) => _repo.GetById(id);
    public void Save(Payment entity) => _repo.Save(entity);
    public void Update(Payment entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


