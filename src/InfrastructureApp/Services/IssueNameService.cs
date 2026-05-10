using InfrastructureApp.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class IssueNameService : IIssueNameService
    {
        private readonly ApplicationDbContext _db;

        public const int NamingThreshold = 3;

        public static readonly string[] Names =
        [
            "Pothole Pauly", "Sinkhole Sam", "Leaky Larry", "Rusty Rita",
            "Flickering Fran", "Crumble Carlos", "Busted Bob", "Wobbly Wanda",
            "Grimy Greg", "Soggy Steve", "Cracked Craig", "Muddy Marge",
            "Slippery Sally", "Drafty Derek", "Bumpy Betty", "Dingy Dave",
            "Peeling Pete", "Weedy Walt", "Mossy Mort", "Faded Frank",
            "Dodgy Doug", "Smelly Shelly", "Dusty Diana", "Gritty Gary",
            "Stormy Stan", "Crumbling Clarence", "Droopy Donna", "Jangly Jeff",
            "Rattling Reggie", "Spluttering Sylvia"
        ];

        public IssueNameService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> AssignNameAsync(int reportId, string name)
        {
            if (!Names.Contains(name)) return false;

            var report = await _db.ReportIssue.FindAsync(reportId);
            if (report == null || report.IssueName != null) return false;

            int voteCount = await _db.ReportVotes.CountAsync(v => v.ReportIssueId == reportId);
            if (voteCount < NamingThreshold) return false;

            report.IssueName = name;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
