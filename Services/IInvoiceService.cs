using BusinessObjects;

namespace Services;

public interface IInvoiceService
{
    List<Invoice> GetAll();
    Invoice? GetById(int id);
    void Save(Invoice entity);
    void Update(Invoice entity);
    void Delete(int id);
    List<Invoice> Search(string? keyword, string? status);
    bool IsEnrollmentOwnedByStudent(int enrollmentId, int studentId);
    bool HasOpenInvoiceForEnrollment(int enrollmentId, int? excludedInvoiceId = null);
    decimal GetPaidAmount(int invoiceId);
    bool HasPayments(int invoiceId);
}


