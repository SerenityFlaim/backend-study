using System.Text;
using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;
using Project.Common;

namespace Consumer.Clients;

public class OmsClient
{
    private readonly HttpClient _client;
    private readonly ILogger<OmsClient> _logger;

    public OmsClient(HttpClient client, ILogger<OmsClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<V1AuditLogOrderResponse> LogOrder(V1AuditLogOrderRequest request, CancellationToken token)
    {
        var json = request.ToJson();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending request to OMS: {RequestBody}", json);

        HttpResponseMessage msg;
        try
        {
            msg = await _client.PostAsync("api/v1/audit/log-order/batch-create", content, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send request to OMS");
            throw;
        }

        var responseBody = await msg.Content.ReadAsStringAsync(token);
        if (msg.IsSuccessStatusCode)
        {
            _logger.LogInformation("OMS response success: {ResponseBody}", responseBody);
            return responseBody.FromJson<V1AuditLogOrderResponse>();
        }
        else
        {
            _logger.LogError("OMS response failed: {StatusCode}, {ResponseBody}", msg.StatusCode, responseBody);
            throw new HttpRequestException($"OMS request failed. StatusCode: {msg.StatusCode}, Response: {responseBody}");
        }
    }
}