using System;

namespace BusinessObjects;

// WalletTransaction — domain model.

public partial class WalletTransaction
{
    public int TransactionId { get; set; }

    public int StudentId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = null!;

    public string? ProviderOrderId { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Student Student { get; set; } = null!;
}
