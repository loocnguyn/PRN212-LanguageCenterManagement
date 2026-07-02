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
    public List<Invoice> Search(string? keyword, string? status) => _repo.Search(keyword, status);
    public bool IsEnrollmentOwnedByStudent(int enrollmentId, int studentId) => _repo.IsEnrollmentOwnedByStudent(enrollmentId, studentId);
    public bool HasOpenInvoiceForEnrollment(int enrollmentId, int? excludedInvoiceId = null) => _repo.HasOpenInvoiceForEnrollment(enrollmentId, excludedInvoiceId);
    public decimal GetPaidAmount(int invoiceId) => _repo.GetPaidAmount(invoiceId);
    public bool HasPayments(int invoiceId) => _repo.HasPayments(invoiceId);
}


