using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int StudentId { get; set; }

    public int? EnrollmentId { get; set; }

    public decimal OriginalAmount { get; set; }

    public int? DiscountId { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly? DueDate { get; set; }

    public DateOnly? DiscountDeadline { get; set; }

    public string DiscountStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual Enrollment? Enrollment { get; set; }

    public virtual TuitionDiscount? Discount { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;
}
