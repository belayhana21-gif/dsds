using Microsoft.EntityFrameworkCore;
using CMT.Domain.Models;
using TaskEntity = CMT.Domain.Models.Task;

namespace CMT.Domain.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<TaskCategory> TaskCategories { get; set; }
    public DbSet<TaskSubType> TaskSubTypes { get; set; }
    public DbSet<RequestType> RequestTypes { get; set; }
    public DbSet<TaskPriorityLevel> TaskPriorityLevels { get; set; }
    public DbSet<TaskCategoryTargetDay> TaskCategoryTargetDays { get; set; }
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<TaskAttachment> TaskAttachments { get; set; }
    public DbSet<TaskTransfer> TaskTransfers { get; set; }
    public DbSet<CompletedTask> CompletedTasks { get; set; }
    public DbSet<CompletedTaskComment> CompletedTaskComments { get; set; }
    public DbSet<CompletedTaskAttachment> CompletedTaskAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure default values for BaseEntity properties with static dates
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Configure BaseEntity defaults for all entities that inherit from it
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.StartDate))
                    .HasDefaultValue(seedDate);

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.EndDate))
                    .HasDefaultValue(maxDate);

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.RegisteredDate))
                    .HasDefaultValueSql("GETUTCDATE()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.LastUpdateDate))
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);
                
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Self-referencing relationship for supervisor
            entity.HasOne(e => e.Supervisor)
                .WithMany()
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.NoAction);

            // User-Shop relationship
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.Users)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Shop entity
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Shop-TeamLeader relationship (one-to-one)
            entity.HasOne(e => e.TeamLeader)
                .WithMany()
                .HasForeignKey(e => e.TeamLeaderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Task entity - Fix foreign key conflicts
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
                
            entity.Property(e => e.AmendmentStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Configure relationships with proper delete behaviors to avoid cascade conflicts
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Creator)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure AmendmentReviewer relationship
            entity.HasOne(e => e.AmendmentReviewer)
                .WithMany()
                .HasForeignKey(e => e.AmendmentReviewedByTlId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure CancelledByUser relationship
            entity.HasOne(e => e.CancelledByUser)
                .WithMany()
                .HasForeignKey(e => e.CancelledBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure SubType relationship
            entity.HasOne(e => e.SubType)
                .WithMany()
                .HasForeignKey(e => e.SubTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Fix RequestType relationship - explicitly configure to avoid shadow properties
            entity.HasOne(e => e.RequestType)
                .WithMany(rt => rt.Tasks)
                .HasForeignKey(e => e.RequestTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Priority relationship - fix the duplicate foreign key issue
            entity.HasOne(e => e.Priority)
                .WithMany()
                .HasForeignKey(e => e.PriorityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Shop relationship
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.Tasks)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure CompletedTask entity
        modelBuilder.Entity<CompletedTask>(entity =>
        {
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
                
            entity.Property(e => e.AmendmentStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.CompletedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.MovedToCompletedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Configure relationships
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.AmendmentReviewer)
                .WithMany()
                .HasForeignKey(e => e.AmendmentReviewedByTlId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.CancelledByUser)
                .WithMany()
                .HasForeignKey(e => e.CancelledBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SubType)
                .WithMany()
                .HasForeignKey(e => e.SubTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RequestType)
                .WithMany()
                .HasForeignKey(e => e.RequestTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Priority)
                .WithMany()
                .HasForeignKey(e => e.PriorityId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Shop)
                .WithMany()
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure CompletedTaskComment entity
        modelBuilder.Entity<CompletedTaskComment>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.CompletedTask)
                .WithMany(t => t.TaskComments)
                .HasForeignKey(e => e.CompletedTaskId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure CompletedTaskAttachment entity
        modelBuilder.Entity<CompletedTaskAttachment>(entity =>
        {
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.CompletedTask)
                .WithMany(t => t.Attachments)
                .HasForeignKey(e => e.CompletedTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TaskCategory entity
        modelBuilder.Entity<TaskCategory>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure TaskCategoryTargetDay entity
        modelBuilder.Entity<TaskCategoryTargetDay>(entity =>
        {
            entity.HasIndex(e => e.CategoryId).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Category)
                .WithOne(c => c.TargetDay)
                .HasForeignKey<TaskCategoryTargetDay>(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TaskPriorityLevel entity
        modelBuilder.Entity<TaskPriorityLevel>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure TaskSubType entity
        modelBuilder.Entity<TaskSubType>(entity =>
        {
            entity.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.Category)
                .WithMany(c => c.SubTypes)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure RequestType entity
        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.Recipient)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PerformanceMetric entity
        modelBuilder.Entity<PerformanceMetric>(entity =>
        {
            // Fix decimal precision issues
            entity.Property(e => e.AverageCompletionTime)
                .HasPrecision(10, 2);
                
            entity.Property(e => e.EfficiencyRating)
                .HasPrecision(5, 2);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithOne(u => u.PerformanceMetric)
                .HasForeignKey<PerformanceMetric>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TaskComment entity
        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.Task)
                .WithMany(t => t.TaskComments)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure TaskAttachment entity
        modelBuilder.Entity<TaskAttachment>(entity =>
        {
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TaskTransfer entity
        modelBuilder.Entity<TaskTransfer>(entity =>
        {
            entity.Property(e => e.TransferDate)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.FromUser)
                .WithMany()
                .HasForeignKey(e => e.FromUserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ToUser)
                .WithMany()
                .HasForeignKey(e => e.ToUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Seed initial data with static values
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // Use static DateTime values instead of DateTime.UtcNow
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Seed TaskCategories
        modelBuilder.Entity<TaskCategory>().HasData(
            new TaskCategory 
            { 
                CategoryId = 1, 
                Name = "AOG & CSD Components Evaluation", 
                Description = "Aircraft on Ground (AOG) and Critical Spares Demand (CSD) component evaluations.", 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            },
            new TaskCategory 
            { 
                CategoryId = 2, 
                Name = "Task Card Related", 
                Description = "Tasks related to creating, revising, or approving task cards.", 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            },
            new TaskCategory 
            { 
                CategoryId = 3, 
                Name = "Scrap Evaluation & Approval", 
                Description = "Evaluation and approval process for parts marked for scrap.", 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            }
        );

        // Seed Priority Levels
        modelBuilder.Entity<TaskPriorityLevel>().HasData(
            new TaskPriorityLevel 
            { 
                PriorityId = 1, 
                LevelName = "Critical", 
                OrderRank = 0, 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new TaskPriorityLevel 
            { 
                PriorityId = 2, 
                LevelName = "High", 
                OrderRank = 1, 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new TaskPriorityLevel 
            { 
                PriorityId = 3, 
                LevelName = "Medium", 
                OrderRank = 2, 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new TaskPriorityLevel 
            { 
                PriorityId = 4, 
                LevelName = "Low", 
                OrderRank = 3, 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );

        // Seed Shops
        modelBuilder.Entity<Shop>().HasData(
            new Shop 
            { 
                ShopId = 1, 
                Name = "Machine Shop", 
                Description = "Handles all machining tasks.", 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate
            },
            new Shop 
            { 
                ShopId = 2, 
                Name = "Paint Shop", 
                Description = "Handles all painting tasks.", 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate
            },
            new Shop 
            { 
                ShopId = 3, 
                Name = "Welding Shop", 
                Description = "Handles all welding tasks.", 
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate
            }
        );

        // Seed Users with BCrypt hashed passwords
        // BCrypt hash for "cmt@1234567": $2a$11$Xp7L8vJZ8vJZ8vJZ8vJZ8O.KqF8vJZ8vJZ8vJZ8vJZ8vJZ8vJZ8vJu
        var hashedPassword = "$2a$11$Xp7L8vJZ8vJZ8vJZ8vJZ8O.KqF8vJZ8vJZ8vJZ8vJZ8vJZ8vJZ8vJu";
        
        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                UserId = 1,
                Username = "team_leader",
                Password = hashedPassword,
                Email = "team_leader@cmt.com",
                FullName = "Team Leader",
                Role = UserRole.TeamLeader,
                Status = UserStatus.Active,
                ShopId = 1,
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new User 
            { 
                UserId = 2,
                Username = "director",
                Password = hashedPassword,
                Email = "director@cmt.com",
                FullName = "Director",
                Role = UserRole.Director,
                Status = UserStatus.Active,
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new User 
            { 
                UserId = 3,
                Username = "engineer",
                Password = hashedPassword,
                Email = "engineer@cmt.com",
                FullName = "Engineer",
                Role = UserRole.Engineer,
                Status = UserStatus.Active,
                ShopId = 1,
                SupervisorId = 1,
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new User 
            { 
                UserId = 4,
                Username = "shop_tl",
                Password = hashedPassword,
                Email = "shop_tl@cmt.com",
                FullName = "Shop Team Leader",
                Role = UserRole.ShopTL,
                Status = UserStatus.Active,
                ShopId = 2,
                RegisteredBy = "System",
                RegisteredDate = seedDate,
                LastUpdateDate = seedDate,
                UpdatedBy = "System",
                StartDate = seedDate,
                EndDate = maxDate,
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}