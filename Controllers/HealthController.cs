using HoneypotAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoneypotAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public HealthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Check database connectivity
                var canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        timestamp = DateTime.UtcNow,
                        components = new
                        {
                            database = "unhealthy"
                        },
                        message = "Database connection failed"
                    });
                }

                // Get basic stats
                var totalRequests = await _context.Requests.CountAsync();
                var totalResponses = await _context.Responses.CountAsync();

                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "Honeypot API",
                    version = "1.0.0",
                    components = new
                    {
                        database = new
                        {
                            status = "healthy",
                            total_requests = totalRequests,
                            total_responses = totalResponses
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    components = new
                    {
                        database = "unhealthy"
                    },
                    message = ex.Message
                });
            }
        }
    }
}
