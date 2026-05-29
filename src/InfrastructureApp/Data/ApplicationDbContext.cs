using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InfrastructureApp.Models;

namespace InfrastructureApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<Users>
    {
        //Db context options are provided by dependency injection at runtime
        //The base constructor must be called so IdentityDbContext can 
        //conigure its requires schema
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        //The null forgiving operator tells the compiler that E Core 
        //will initialize this property at runtime.
        public DbSet<UserPoints> UserPoints { get; set; } = null!;

        public DbSet<MinigamePlay> MinigamePlays { get; set; } = null!;

        public DbSet<ShopItem> ShopItems { get; set; } = null!;

        public DbSet<UserShopItemPurchase> UserShopItemPurchases { get; set; } = null!;

        //this maps the reportIssue to the reports table, used for CRUD operations in EF
        public DbSet<ReportIssue> ReportIssue { get; set; } = null!;

        public DbSet<ReportVote> ReportVotes { get; set; } = null!;

        public DbSet<ReportVerification> ReportVerifications { get; set; } = null!;

        public DbSet<ReportFlag> ReportFlags { get; set; } = null!;

        public DbSet<ModerationActionLog> ModerationActionLogs { get; set; } = null!;

        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public DbSet<ReportStatusHistory> ReportStatusHistory { get; set; } = null!;

        //This is the place for constraints, defaults, indexes, and relationships
        protected override void OnModelCreating(ModelBuilder builder)
        {

            //We always call base implementation first
            //This ensures tables and relationships are correctly
            //configured before adding custom mappings
            base.OnModelCreating(builder);

            //Enforces a Unique contraint on UserId in the UserPoints table.
            //This guarantees a one to one relationship
            builder.Entity<UserPoints>()
                .HasIndex(up => up.UserId)
                .IsUnique();


            //Sets a database level default value for currentpoints
            //If a new UserPoints record is inserted without explicitly 
            //setting CurrentPoints, SQL Server will default it to 0
            builder.Entity<UserPoints>()
                .Property(up => up.CurrentPoints)
                .HasDefaultValue(0);

            //Sets database level default value for LifetimePoints.
            //This allows point accumulation Logic to assume a starting value
            //without requiring application side initialization
            builder.Entity<UserPoints>()
                .Property(up => up.LifetimePoints)
                .HasDefaultValue(0);


            //Uses SQL server's SYSUTCDATETIME() funnction to automatically
            //populate the LastUpdated column when a row is created
            //Ensures consistency
            builder.Entity<UserPoints>()
                .Property(up => up.LastUpdated)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Entity<MinigamePlay>(entity =>
            {
                entity.Property(play => play.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(play => play.GameKey)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(play => play.PlayedOnDate)
                    .HasColumnType("date");

                entity.Property(play => play.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(play => new { play.UserId, play.GameKey, play.PlayedOnDate })
                    .IsUnique();
            });

            builder.Entity<ShopItem>(entity =>
            {
                entity.Property(i => i.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(i => i.Description)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(i => i.CostPoints)
                    .IsRequired();

                entity.Property(i => i.IsSinglePurchase)
                    .HasDefaultValue(true);

                entity.Property(i => i.IsActive)
                    .HasDefaultValue(true);

                entity.Property(i => i.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(i => i.Name)
                    .IsUnique();
            });

            builder.Entity<UserShopItemPurchase>(entity =>
            {
                entity.Property(p => p.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(p => p.CostPoints)
                    .IsRequired();

                entity.Property(p => p.PurchasedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(p => p.ShopItem)
                    .WithMany()
                    .HasForeignKey(p => p.ShopItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => new { p.UserId, p.ShopItemId, p.PurchasedAt });
            });



            //report issue maps to existing SQL table reports
            builder.Entity<ReportIssue>(entity =>
            {
                // Map to existing SQL table
                entity.ToTable("Reports");

                // Primary key
                entity.HasKey(r => r.Id);

                // Column mappings
                //description required
                entity.Property(r => r.Description)
                    .IsRequired();

                //status required
                entity.Property(r => r.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(r => r.CreatedAt)
                    .HasColumnType("datetime2");

                // Foreign key to the Identity user who submitted the report
                entity.Property(r => r.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.Property(r => r.Latitude)
                    .HasColumnType("decimal(9,6)");

                entity.Property(r => r.Longitude)
                    .HasColumnType("decimal(9,6)");

                entity.Property(r => r.ImageUrl)
                    .HasMaxLength(450);

                // SHA-256 is stored as a 64-character hex string.
                entity.Property(r => r.ImageSha256)
                    .HasMaxLength(64);

                // pHash stored as SQL bigint through C# long.
                entity.Property(r => r.ImagePHash);

                // Helpful for exact duplicate checks:
                // "Has this same user already uploaded this same exact image?"
                entity.HasIndex(r => new { r.UserId, r.ImageSha256 });
            });

            // One vote per user per report
            builder.Entity<ReportVote>()
                .HasIndex(v => new { v.ReportIssueId, v.UserId })
                .IsUnique();

            // One verification per user per report
            builder.Entity<ReportVerification>()
                .HasIndex(v => new { v.ReportIssueId, v.UserId })
                .IsUnique();

            // One flag per user per report
            builder.Entity<ReportFlag>()
                .HasIndex(f => new { f.ReportIssueId, f.UserId })
                .IsUnique();

            builder.Entity<ReportStatusHistory>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(h => h.ChangedAt)
                    .HasColumnType("datetime2")
                    .IsRequired();

                entity.Property(h => h.ChangedByUserId)
                    .HasMaxLength(450);

                entity.Property(h => h.ChangedByDisplayName)
                    .HasMaxLength(256);

                entity.HasOne(h => h.ReportIssue)
                    .WithMany()
                    .HasForeignKey(h => h.ReportIssueId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");

                entity.Property(log => log.AspNetUserId)
                    .HasMaxLength(450);

                entity.Property(log => log.UserName)
                    .HasMaxLength(256);

                entity.Property(log => log.Email)
                    .HasMaxLength(256);

                entity.Property(log => log.Role)
                    .HasMaxLength(256);

                entity.Property(log => log.TimestampUtc)
                    .IsRequired();

                entity.Property(log => log.Action)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

        }
    }
}
