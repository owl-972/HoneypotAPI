using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoneypotAPI.Data;
using HoneypotAPI.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HoneypotAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class HoneypotController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HoneypotController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("{**endpoint}")]
        public async Task<IActionResult> GetProxy(string endpoint)
        {
            return await ProcessRequest(endpoint, "GET");
        }

        [HttpPost("{**endpoint}")]
        public async Task<IActionResult> PostProxy(string endpoint)
        {
            return await ProcessRequest(endpoint, "POST");
        }

        [HttpPut("{**endpoint}")]
        public async Task<IActionResult> PutProxy(string endpoint)
        {
            return await ProcessRequest(endpoint, "PUT");
        }

        [HttpDelete("{**endpoint}")]
        public async Task<IActionResult> DeleteProxy(string endpoint)
        {
            return await ProcessRequest(endpoint, "DELETE");
        }

        private async Task<IActionResult> ProcessRequest(string endpoint, string method)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Phase 1: Log the incoming request
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var headers = JsonSerializer.Serialize(Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

                string payload = null;
                if (Request.ContentLength > 0)
                {
                    using var reader = new StreamReader(Request.Body);
                    payload = await reader.ReadToEndAsync();
                }

                var requestLog = new Request
                {
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    Endpoint = $"/{endpoint}",
                    HttpMethod = method,
                    Headers = headers,
                    Payload = payload
                };

                _context.Requests.Add(requestLog);
                await _context.SaveChangesAsync();
                var requestId = requestLog.Id;

                // Phase 2: Forward to real system
                var realSystemBaseUrl = _configuration["RealSystemConfig:BaseUrl"];
                var targetUrl = $"{realSystemBaseUrl}/{endpoint.Replace("-", "/")}";

                var client = _httpClientFactory.CreateClient();
                var httpRequest = new HttpRequestMessage(new HttpMethod(method), targetUrl);

                // Copy headers
                foreach (var header in Request.Headers)
                {
                    if (!header.Key.StartsWith("Content-") && header.Key != "Host")
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // Add body for POST/PUT
                if (!string.IsNullOrEmpty(payload) && (method == "POST" || method == "PUT"))
                {
                    httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                }

                // Send request and measure time
                var response = await client.SendAsync(httpRequest);
                stopwatch.Stop();

                // Phase 3: Log the response
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseLog = new Response
                {
                    RequestId = requestId,
                    ResponseStatus = (int)response.StatusCode,
                    ResponsePayload = responseContent,
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds
                };

                _context.Responses.Add(responseLog);
                await _context.SaveChangesAsync();

                // Phase 4: Return response to client
                return StatusCode((int)response.StatusCode, new
                {
                    requestId = requestId,
                    status = (int)response.StatusCode,
                    data = string.IsNullOrEmpty(responseContent) ? null : JsonSerializer.Deserialize<object>(responseContent)
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log error response
                var errorResponse = new Response
                {
                    RequestId = _context.Requests.OrderByDescending(r => r.Id).First().Id,
                    ResponseStatus = 500,
                    ResponsePayload = JsonSerializer.Serialize(new { error = ex.Message }),
                    ResponseTime = (int)stopwatch.ElapsedMilliseconds
                };

                _context.Responses.Add(errorResponse);
                await _context.SaveChangesAsync();

                return StatusCode(500, new
                {
                    error = "Error forwarding request to real system",
                    message = ex.Message
                });
            }
        }
    }
}