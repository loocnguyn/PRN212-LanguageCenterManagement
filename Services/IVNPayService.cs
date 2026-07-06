namespace Services;

public interface IVNPayService
{
    /// <summary>Builds the VNPay payment URL (browser is redirected here, no QR/app needed for testing).</summary>
    string CreatePaymentUrl(string orderId, decimal amount, string orderInfo, string clientIpAddress);

    /// <summary>Validates the vnp_SecureHash on the ReturnUrl query string after the user pays.</summary>
    bool ValidateReturnSignature(IDictionary<string, string> queryParams);
}
