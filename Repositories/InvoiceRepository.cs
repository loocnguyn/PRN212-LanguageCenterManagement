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
    public List<Invoice> Search(string? keyword, string? status) => InvoiceDAO.Search(keyword, status);
    public bool IsEnrollmentOwnedByStudent(int enrollmentId, int studentId) => InvoiceDAO.IsEnrollmentOwnedByStudent(enrollmentId, studentId);
    public bool HasOpenInvoiceForEnrollment(int enrollmentId, int? excludedInvoiceId = null) => InvoiceDAO.HasOpenInvoiceForEnrollment(enrollmentId, excludedInvoiceId);
    public decimal GetPaidAmount(int invoiceId) => InvoiceDAO.GetPaidAmount(invoiceId);
    public bool HasPayments(int invoiceId) => InvoiceDAO.HasPayments(invoiceId);
}


