using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Services;

public class MoMoService : IMoMoService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _partnerCode;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _endpoint;
    private readonly string _queryEndpoint;
    private readonly string _redirectUrl;
    private readonly string _ipnUrl;

    public MoMoService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var section = config.GetSection("MoMo");
        _partnerCode = section["PartnerCode"] ?? "MOMO";
        _accessKey = section["AccessKey"] ?? "F8BBA842ECF85";
        _secretKey = section["SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        _endpoint = section["Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
        _queryEndpoint = section["QueryEndpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/query";
        _redirectUrl = section["RedirectUrl"] ?? "https://momo.vn/return";
        _ipnUrl = section["IpnUrl"] ?? "https://momo.vn/notify";
    }

    public async Task<MoMoCreatePaymentResult> CreatePaymentAsync(string orderId, decimal amount, string orderInfo)
    {
        var requestId = Guid.NewGuid().ToString();
        var amountStr = ((long)amount).ToString();
        const string requestType = "captureWallet";
        const string extraData = "";

        var rawSignature =
            $"accessKey={_accessKey}&amount={amountStr}&extraData={extraData}" +
            $"&ipnUrl={_ipnUrl}&orderId={orderId}&orderInfo={orderInfo}" +
            $"&partnerCode={_partnerCode}&redirectUrl={_redirectUrl}" +
            $"&requestId={requestId}&requestType={requestType}";

        var signature = ComputeHmacSha256(rawSignature, _secretKey);

        var requestBody = new
        {
            partnerCode = _partnerCode,
            requestId,
            amount = amountStr,
            orderId,
            orderInfo,
            redirectUrl = _redirectUrl,
            ipnUrl = _ipnUrl,
            extraData,
            requestType,
            signature,
            lang = "vi"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
        var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
        var payUrl = root.TryGetProperty("payUrl", out var url) ? url.GetString() : null;

        return new MoMoCreatePaymentResult
        {
            Success = resultCode == 0,
            PayUrl = payUrl,
            ResultCode = resultCode,
            Message = message
        };
    }

    public async Task<MoMoQueryResult> QueryTransactionStatusAsync(string orderId)
    {
        var requestId = Guid.NewGuid().ToString();

        var rawSignature =
            $"accessKey={_accessKey}&orderId={orderId}&partnerCode={_partnerCode}&requestId={requestId}";
        var signature = ComputeHmacSha256(rawSignature, _secretKey);

        var requestBody = new
        {
            partnerCode = _partnerCode,
            requestId,
            orderId,
            signature,
            lang = "vi"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_queryEndpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
        var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
        var amount = root.TryGetProperty("amount", out var amt) ? amt.GetInt64() : 0;

        return new MoMoQueryResult
        {
            ResultCode = resultCode,
            Message = message,
            Amount = amount
        };
    }

    private static string ComputeHmacSha256(string message, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
