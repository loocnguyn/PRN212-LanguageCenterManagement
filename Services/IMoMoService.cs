namespace Services;

public interface IMoMoService
{
    Task<MoMoCreatePaymentResult> CreatePaymentAsync(string orderId, decimal amount, string orderInfo);
    Task<MoMoQueryResult> QueryTransactionStatusAsync(string orderId);
}
