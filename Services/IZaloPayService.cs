namespace Services;

public interface IZaloPayService
{
    /// <summary>Creates a ZaloPay order and returns the order_url to open in a browser (has its own QR).</summary>
    Task<ZaloPayCreateResult> CreateOrderAsync(string appTransId, decimal amount, string description);

    /// <summary>Queries the current status of a previously created order.</summary>
    Task<ZaloPayQueryResult> QueryOrderStatusAsync(string appTransId);
}

public class ZaloPayCreateResult
{
    public bool Success { get; set; }
    public string? OrderUrl { get; set; }
    public int ReturnCode { get; set; }
    public string? Message { get; set; }
}

public class ZaloPayQueryResult
{
    /// <summary>1 = paid successfully, 2 = failed/cancelled, 3 = pending (per ZaloPay convention).</summary>
    public int ReturnCode { get; set; }
    public string? Message { get; set; }

    public bool IsSuccess => ReturnCode == 1;
    public bool IsPending => ReturnCode == 3;
}
