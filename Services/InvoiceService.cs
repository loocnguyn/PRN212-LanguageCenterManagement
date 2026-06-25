using BusinessObjects;
using Repositories;

namespace Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo = new InvoiceRepository();

    public List<Invoice> GetAll() => _repo.GetAll();
    public Invoice? GetById(int id) => _repo.GetById(id);
    public void Save(Invoice entity) => _repo.Save(entity);
    public void Update(Invoice entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


