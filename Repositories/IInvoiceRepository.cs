using BusinessObjects;

namespace Repositories;

public interface IInvoiceRepository
{
    List<Invoice> GetAll();
    Invoice? GetById(int id);
    void Save(Invoice entity);
    void Update(Invoice entity);
    void Delete(int id);
}


