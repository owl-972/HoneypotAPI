using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoneypotAPI.Data;
using System.Text.Json;

namespace HoneypotAPI.Controllers
{
    [ApiController]
    [Route("audit")]
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string ipAddress = null,
            [FromQuery] string endpoint = null,
            [FromQuery] int? statusCode = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            var query = _context.Requests.Include(r => r.Response).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(r => r.IpAddress == ipAddress);

            if (!string.IsNullOrEmpty(endpoint))
                query = query.Where(r => r.Endpoint.Contains(endpoint));

            if (statusCode.HasValue)
                query = query.Where(r => r.Response.ResponseStatus == statusCode.Value);

            if (dateFrom.HasValue)
                query = query.Where(r => r.Timestamp >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(r => r.Timestamp <= dateTo.Value);

            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(r => r.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    requestId = r.Id,
                    ipAddress = r.IpAddress,
                    timestamp = r.Timestamp,
                    endpoint = r.Endpoint,
                    httpMethod = r.HttpMethod,
                    headers = r.Headers,
                    payload = r.Payload,
                    response = r.Response == null ? null : new
                    {
                        responseId = r.Response.Id,
                        responseStatus = r.Response.ResponseStatus,
                        responsePayload = r.Response.ResponsePayload,
                        responseTime = r.Response.ResponseTime
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                totalRecords,
                page,
                pageSize,
                data
            });
        }

        [HttpDelete("requests/{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Response)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Request not found"
                });
            }

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Request and associated response deleted successfully",
                deletedRequestId = id
            });
        }
    }
}