//Defines the function listed in IReportIssueRepository

using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class ReportIssueRepositoryEf : IReportIssueRepository
    {
        private readonly ApplicationDbContext _db;

        // Inject DbContext through DI
        public ReportIssueRepositoryEf(ApplicationDbContext db)
        {
            _db = db;
        }

        // Existing repo operations (used by other report features)
        //queries the reports table and returns report if found
        public Task<ReportIssue?> GetByIdAsync(int id)
        {
            return _db.ReportIssue.FirstOrDefaultAsync(r => r.Id == id);
        }

        //adds a new report report to EF's change tracker
        public async Task AddAsync(ReportIssue report)
        {
            _db.ReportIssue.Add(report);
            await Task.CompletedTask;
        }

        //saves changes to the database, inserts reports row into the reports table
        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }

        // Returns latest reports based on visibility rules
        public async Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin)
        {
            // Start building query from ReportIssue table
            var query = _db.ReportIssue.AsQueryable();

            // Apply visibility filtering (approved vs all)
            query = ReportIssue.VisibleToUser(query, isAdmin);

            // Sort newest reports first
            query = ReportIssue.OrderLatestFirst(query);

            // Execute query and return results
            return await query.ToListAsync();
        }

        public async Task<List<ReportIssue>> GetResolvedReportsAsync()
        {
            return await _db.ReportIssue
                .Where(r => r.Status == "Resolved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Originally feature83
        // Returns latest reports filtered by user visibility, search keyword, and sort order
        // SCRUM-86 UPDATED: added sort support using ApplyDateSort helper
        public async Task<List<ReportIssue>> SearchLatestReportsAsync(bool isAdmin, string? keyword, string? sort)
        {
            // Start query from ReportIssue table
            var query = _db.ReportIssue.AsQueryable();

            // Apply visibility rule (approved vs all)
            query = ReportIssue.VisibleToUser(query, isAdmin);

            // Apply keyword search on description
            query = ReportIssue.FilterByDescription(query, keyword);

            // SCRUM-86 UPDATED: apply newest/oldest sort (default newest)
            query = ReportIssue.ApplyDateSort(query, sort);

            // Execute query and return results
            return await query.ToListAsync();
        }
    }
}
