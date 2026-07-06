using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Services;

public class ZaloPayService : IZaloPayService
{
    private readonly HttpClient _httpClient = new();
    private readonly string _appId;
    private readonly string _key1;
    private readonly string _createEndpoint;
    private readonly string _queryEndpoint;

    public ZaloPayService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var section = config.GetSection("ZaloPay");
        _appId = section["AppId"] ?? "2553";
        _key1 = section["Key1"] ?? "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL";
        _createEndpoint = section["CreateEndpoint"] ?? "https://sb-openapi.zalopay.vn/v2/create";
        _queryEndpoint = section["QueryEndpoint"] ?? "https://sb-openapi.zalopay.vn/v2/query";
    }

    public async Task<ZaloPayCreateResult> CreateOrderAsync(string appTransId, decimal amount, string description)
    {
        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var appUser = "student";
        const string embedData = "{}";
        const string item = "[]";
        var amountStr = ((long)amount).ToString();

        var macInput = $"{_appId}|{appTransId}|{appUser}|{amountStr}|{appTime}|{embedData}|{item}";
        var mac = ComputeHmacSha256(macInput, _key1);

        var formData = new Dictionary<string, string>
        {
            ["app_id"] = _appId,
            ["app_trans_id"] = appTransId,
            ["app_user"] = appUser,
            ["app_time"] = appTime,
            ["amount"] = amountStr,
            ["item"] = item,
            ["description"] = description,
            ["embed_data"] = embedData,
            ["mac"] = mac
        };

        using var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(_createEndpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var returnCode = root.TryGetProperty("return_code", out var rc) ? rc.GetInt32() : -1;
        var message = root.TryGetProperty("return_message", out var msg) ? msg.GetString() : null;
        var orderUrl = root.TryGetProperty("order_url", out var url) ? url.GetString() : null;

        return new ZaloPayCreateResult
        {
            Success = returnCode == 1,
            OrderUrl = orderUrl,
            ReturnCode = returnCode,
            Message = message
        };
    }

    public async Task<ZaloPayQueryResult> QueryOrderStatusAsync(string appTransId)
    {
        var macInput = $"{_appId}|{appTransId}|{_key1}";
        var mac = ComputeHmacSha256(macInput, _key1);

        var formData = new Dictionary<string, string>
        {
            ["app_id"] = _appId,
            ["app_trans_id"] = appTransId,
            ["mac"] = mac
        };

        using var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(_queryEndpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var returnCode = root.TryGetProperty("return_code", out var rc) ? rc.GetInt32() : -1;
        var message = root.TryGetProperty("return_message", out var msg) ? msg.GetString() : null;

        return new ZaloPayQueryResult { ReturnCode = returnCode, Message = message };
    }

    private static string ComputeHmacSha256(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
