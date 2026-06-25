using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    public List<Invoice> GetAll() => InvoiceDAO.GetAll();
    public Invoice? GetById(int id) => InvoiceDAO.GetById(id);
    public void Save(Invoice entity) => InvoiceDAO.Save(entity);
    public void Update(Invoice entity) => InvoiceDAO.Update(entity);
    public void Delete(int id) => InvoiceDAO.Delete(id);
}


