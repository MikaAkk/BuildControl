using Microsoft.AspNetCore.Http;
using WebBuild.Models;
using WebBuild.Models.Enities;

namespace WebBuild.Service
{
    public class AuditLogService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthService _auth;

        public AuditLogService(AppDbContext db, IHttpContextAccessor httpContextAccessor, AuthService auth)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _auth = auth;
        }

        public async Task LogAsync(string action, string entityType, long? entityId, string details = "")
        {
            var employeeId = _auth.GetCurrentUserId();
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var log = new AuditLog
            {
                EmployeeId = employeeId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}
