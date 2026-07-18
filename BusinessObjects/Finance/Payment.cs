using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public int? StaffId { get; set; }

    public decimal AmountPaid { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public DateTime PaidAt { get; set; }

    public string? ReceiptCode { get; set; }

    public string? Note { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Staff? Staff { get; set; }
}
