using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int StudentId { get; set; }

    public int? EnrollmentId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual Enrollment? Enrollment { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;
}
