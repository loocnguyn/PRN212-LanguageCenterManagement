namespace Services;

public class MoMoCreatePaymentResult
{
    public bool Success { get; set; }
    public string? PayUrl { get; set; }
    public int ResultCode { get; set; }
    public string? Message { get; set; }
}

public class MoMoQueryResult
{
    public int ResultCode { get; set; }
    public string? Message { get; set; }
    public long Amount { get; set; }

    public bool IsSuccess => ResultCode == 0;
    public bool IsPending => ResultCode == 1000 || ResultCode == 7002;
}
