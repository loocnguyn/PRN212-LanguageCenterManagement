using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class TuitionDiscount
{
    public int DiscountId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? PaymentDeadlineDays { get; set; }

    public string ConditionType { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
