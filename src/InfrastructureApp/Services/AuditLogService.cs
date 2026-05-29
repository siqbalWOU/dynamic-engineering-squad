using System.Security.Claims;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.AuditLogs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class AuditLogService : IAuditLogService
    {
        private const int ActionMaxLength = 1000;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<Users> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            ApplicationDbContext db,
            UserManager<Users> userManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(string action, string? userId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            try
            {
                var principal = _httpContextAccessor.HttpContext?.User;
                var effectiveUserId = userId ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var auditLog = new AuditLog
                {
                    AspNetUserId = effectiveUserId,
                    TimestampUtc = DateTime.UtcNow,
                    Action = action.Length > ActionMaxLength
                        ? action[..ActionMaxLength]
                        : action
                };

                if (!string.IsNullOrWhiteSpace(effectiveUserId))
                {
                    var user = await _userManager.FindByIdAsync(effectiveUserId);
                    if (user != null)
                    {
                        auditLog.UserName = user.UserName;
                        auditLog.Email = user.Email;

                        var roles = await _userManager.GetRolesAsync(user);
                        auditLog.Role = roles.Count > 0
                            ? string.Join(", ", roles.OrderBy(role => role))
                            : null;
                    }
                }

                auditLog.UserName ??= principal?.Identity?.Name;
                auditLog.Email ??= principal?.FindFirstValue(ClaimTypes.Email);
                auditLog.Role ??= ResolveRoleFromClaims(principal);

                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit logging failed for action: {Action}", action);
            }
        }

        public async Task<IReadOnlyList<AuditLog>> GetLatestAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            var normalizedLimit = limit <= 0 ? 100 : Math.Min(limit, 500);

            return await _db.AuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.TimestampUtc)
                .ThenByDescending(log => log.Id)
                .Take(normalizedLimit)
                .ToListAsync(cancellationToken);
        }

        public async Task<AuditLogsIndexViewModel> GetPageAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var normalizedPageSize = pageSize <= 0 ? 50 : pageSize;
            var totalItems = await _db.AuditLogs.CountAsync(cancellationToken);
            var totalPages = totalItems == 0
                ? 1
                : (int)Math.Ceiling(totalItems / (double)normalizedPageSize);

            var safePage = page < 1 ? 1 : page;
            if (safePage > totalPages)
            {
                safePage = totalPages;
            }

            var items = await _db.AuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.TimestampUtc)
                .ThenByDescending(log => log.Id)
                .Skip((safePage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync(cancellationToken);

            return new AuditLogsIndexViewModel
            {
                Items = items,
                CurrentPage = safePage,
                PageSize = normalizedPageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        private static string? ResolveRoleFromClaims(ClaimsPrincipal? principal)
        {
            var roles = principal?
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToArray();

            return roles == null || roles.Length == 0
                ? null
                : string.Join(", ", roles);
        }
    }
}
