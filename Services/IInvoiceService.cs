using BusinessObjects;

namespace Services;

public interface IInvoiceService
{
    List<Invoice> GetAll();
    Invoice? GetById(int id);
    void Save(Invoice entity);
    void Update(Invoice entity);
    void Delete(int id);
}


