using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Services;

public class VNPayService : IVNPayService
{
    private readonly string _tmnCode;
    private readonly string _hashSecret;
    private readonly string _payUrl;
    private readonly string _returnUrl;

    public VNPayService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var section = config.GetSection("VNPay");
        _tmnCode = section["TmnCode"] ?? "DEMOV210";
        _hashSecret = section["HashSecret"] ?? "RAOEXHYVSDDIIENYWSLDIIZTANGKZOAP";
        _payUrl = section["PayUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        _returnUrl = section["ReturnUrl"] ?? "https://localhost/vnpay-return";
    }

    public string CreatePaymentUrl(string orderId, decimal amount, string orderInfo, string clientIpAddress)
    {
        // VNPay expects the amount multiplied by 100 (smallest currency unit), no decimals.
        var vnpAmount = ((long)amount * 100).ToString();

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _tmnCode,
            ["vnp_Amount"] = vnpAmount,
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = orderId,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _returnUrl,
            ["vnp_IpAddr"] = clientIpAddress,
            ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
        };

        var (hashData, query) = BuildHashDataAndQuery(parameters);
        var secureHash = ComputeHmacSha512(hashData, _hashSecret);

        return $"{_payUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    public bool ValidateReturnSignature(IDictionary<string, string> queryParams)
    {
        if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash))
            return false;

        var toVerify = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParams)
        {
            if (kv.Key is "vnp_SecureHash" or "vnp_SecureHashType") continue;
            toVerify[kv.Key] = kv.Value;
        }

        var (hashData, _) = BuildHashDataAndQuery(toVerify);
        var computedHash = ComputeHmacSha512(hashData, _hashSecret);

        return string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static (string hashData, string query) BuildHashDataAndQuery(SortedDictionary<string, string> parameters)
    {
        var hashData = new StringBuilder();
        var query = new StringBuilder();

        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrEmpty(value)) continue;

            var encodedValue = Uri.EscapeDataString(value);
            hashData.Append(key).Append('=').Append(encodedValue).Append('&');
            query.Append(Uri.EscapeDataString(key)).Append('=').Append(encodedValue).Append('&');
        }

        if (hashData.Length > 0) hashData.Length--; // trim trailing '&'
        if (query.Length > 0) query.Length--;

        return (hashData.ToString(), query.ToString());
    }

    private static string ComputeHmacSha512(string message, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
