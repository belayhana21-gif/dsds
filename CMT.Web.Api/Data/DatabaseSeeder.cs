using CMT.Domain.Data;
using CMT.Domain.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CMT.Web.Api.Data
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;

        public DatabaseSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task SeedAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();

                if (!await _context.Shops.AnyAsync())
                {
                    await SeedShopsAsync();
                }

                if (!await _context.TaskCategories.AnyAsync())
                {
                    await SeedTaskCategoriesAsync();
                }

                if (!await _context.TaskPriorityLevels.AnyAsync())
                {
                    await SeedPriorityLevelsAsync();
                }

                if (!await _context.RequestTypes.AnyAsync())
                {
                    await SeedRequestTypesAsync();
                }

                // Always update users to ensure password consistency
                await UpdateUsersPasswordsAsync();

                if (!await _context.Tasks.AnyAsync())
                {
                    await SeedTasksAsync();
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Database seeded successfully with real data!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                throw;
            }
        }

        private async System.Threading.Tasks.Task SeedShopsAsync()
        {
            var shops = new[]
            {
                new Shop 
                { 
                    Name = "Avionics Shop", 
                    Description = "Avionics and Electronics"
                },
                new Shop 
                { 
                    Name = "Engine Shop", 
                    Description = "Engine Maintenance and Repair"
                },
                new Shop 
                { 
                    Name = "Structural Shop", 
                    Description = "Structural Components and Repairs"
                }
            };

            await _context.Shops.AddRangeAsync(shops);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task SeedTaskCategoriesAsync()
        {
            var categories = new[]
            {
                new TaskCategory 
                { 
                    Name = "Engine", 
                    Description = "Engine components and systems"
                },
                new TaskCategory 
                { 
                    Name = "Avionics", 
                    Description = "Avionics and electronic systems"
                },
                new TaskCategory 
                { 
                    Name = "Landing Gear", 
                    Description = "Landing gear components"
                },
                new TaskCategory 
                { 
                    Name = "Hydraulics", 
                    Description = "Hydraulic systems and components"
                }
            };

            await _context.TaskCategories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task SeedPriorityLevelsAsync()
        {
            var priorities = new[]
            {
                new TaskPriorityLevel 
                { 
                    LevelName = "Low", 
                    Description = "Low priority tasks",
                    OrderRank = 1
                },
                new TaskPriorityLevel 
                { 
                    LevelName = "Medium", 
                    Description = "Medium priority tasks",
                    OrderRank = 2
                },
                new TaskPriorityLevel 
                { 
                    LevelName = "High", 
                    Description = "High priority tasks",
                    OrderRank = 3
                },
                new TaskPriorityLevel 
                { 
                    LevelName = "Urgent", 
                    Description = "Urgent priority tasks",
                    OrderRank = 4
                }
            };

            await _context.TaskPriorityLevels.AddRangeAsync(priorities);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task SeedRequestTypesAsync()
        {
            var requestTypes = new[]
            {
                new RequestType 
                { 
                    Name = "Maintenance", 
                    Description = "Regular maintenance tasks"
                },
                new RequestType 
                { 
                    Name = "Repair", 
                    Description = "Repair and fix tasks"
                },
                new RequestType 
                { 
                    Name = "Inspection", 
                    Description = "Inspection and testing tasks"
                },
                new RequestType 
                { 
                    Name = "Modification", 
                    Description = "Modification and upgrade tasks"
                }
            };

            await _context.RequestTypes.AddRangeAsync(requestTypes);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task UpdateUsersPasswordsAsync()
        {
            // Check if users exist and update their passwords to ensure consistency
            var existingUsers = await _context.Users.ToListAsync();
            
            if (existingUsers.Any())
            {
                // Update existing users with properly hashed passwords
                foreach (var user in existingUsers)
                {
                    // Hash the default password for all users
                    user.Password = BCrypt.Net.BCrypt.HashPassword("cmt@1234567", BCrypt.Net.BCrypt.GenerateSalt(12));
                    user.UpdatedAt = DateTime.UtcNow;
                }
                
                Console.WriteLine($"Updated passwords for {existingUsers.Count} existing users");
            }
            else
            {
                // Create new users if none exist
                await SeedUsersAsync();
            }
        }

        private async System.Threading.Tasks.Task SeedUsersAsync()
        {
            var shops = await _context.Shops.ToListAsync();
            var avionicsShop = shops.FirstOrDefault(s => s.Name == "Avionics Shop");
            var engineShop = shops.FirstOrDefault(s => s.Name == "Engine Shop");

            // Generate a consistent salt and hash for all users
            var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("cmt@1234567", salt);

            var users = new[]
            {
                new User
                {
                    Username = "director",
                    Email = "director@cmt.com",
                    FullName = "System Director",
                    Password = hashedPassword,
                    Role = CMT.Domain.Models.UserRole.Director,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "team_leader",
                    Email = "teamlead@cmt.com",
                    FullName = "Team Leader One",
                    Password = hashedPassword,
                    Role = CMT.Domain.Models.UserRole.TeamLeader,
                    ShopId = avionicsShop?.ShopId,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "john_smith",
                    Email = "john.smith@cmt.com",
                    FullName = "John Smith",
                    Password = hashedPassword,
                    Role = CMT.Domain.Models.UserRole.Engineer,
                    ShopId = engineShop?.ShopId,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "sarah_johnson",
                    Email = "sarah.johnson@cmt.com",
                    FullName = "Sarah Johnson",
                    Password = hashedPassword,
                    Role = CMT.Domain.Models.UserRole.Engineer,
                    ShopId = avionicsShop?.ShopId,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "mike_davis",
                    Email = "mike.davis@cmt.com",
                    FullName = "Mike Davis",
                    Password = hashedPassword,
                    Role = CMT.Domain.Models.UserRole.Engineer,
                    ShopId = engineShop?.ShopId,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Created {users.Length} new users with consistent password hashing");
        }

        private async System.Threading.Tasks.Task SeedTasksAsync()
        {
            var shops = await _context.Shops.ToListAsync();
            var categories = await _context.TaskCategories.ToListAsync();
            var priorities = await _context.TaskPriorityLevels.ToListAsync();
            var requestTypes = await _context.RequestTypes.ToListAsync();
            var users = await _context.Users.ToListAsync();

            var engineShop = shops.FirstOrDefault(s => s.Name == "Engine Shop");
            var avionicsShop = shops.FirstOrDefault(s => s.Name == "Avionics Shop");
            
            var engineCategory = categories.FirstOrDefault(c => c.Name == "Engine");
            var avionicsCategory = categories.FirstOrDefault(c => c.Name == "Avionics");
            var landingGearCategory = categories.FirstOrDefault(c => c.Name == "Landing Gear");
            
            var highPriority = priorities.FirstOrDefault(p => p.LevelName == "High");
            var mediumPriority = priorities.FirstOrDefault(p => p.LevelName == "Medium");
            var lowPriority = priorities.FirstOrDefault(p => p.LevelName == "Low");
            
            var maintenanceType = requestTypes.FirstOrDefault(r => r.Name == "Maintenance");
            var repairType = requestTypes.FirstOrDefault(r => r.Name == "Repair");
            var inspectionType = requestTypes.FirstOrDefault(r => r.Name == "Inspection");
            
            var director = users.FirstOrDefault(u => u.Username == "director");
            var teamLeader = users.FirstOrDefault(u => u.Username == "team_leader");

            var tasks = new[]
            {
                new CMT.Domain.Models.Task
                {
                    SerialNumber = "SN-ENG-001",
                    PartNumber = "PN-ENG-7845",
                    PoNumber = "PO-2024-001",
                    Description = "Engine turbine blade inspection and replacement",
                    CategoryId = engineCategory?.CategoryId ?? 1,
                    RequestTypeId = inspectionType?.RequestTypeId ?? 1,
                    Status = CMT.Domain.Models.TaskStatus.InProgress,
                    AssignedEngineer = "John Smith",
                    PriorityId = highPriority?.PriorityId ?? 3,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(7),
                    TargetCompletionDate = DateTime.UtcNow.AddDays(5),
                    ShopId = engineShop?.ShopId ?? 1,
                    CreatedBy = director?.UserId ?? 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow
                },
                new CMT.Domain.Models.Task
                {
                    SerialNumber = "SN-AVI-002",
                    PartNumber = "PN-AVI-3421",
                    PoNumber = "PO-2024-002",
                    Description = "Navigation system calibration and testing",
                    CategoryId = avionicsCategory?.CategoryId ?? 2,
                    RequestTypeId = maintenanceType?.RequestTypeId ?? 1,
                    Status = CMT.Domain.Models.TaskStatus.Pending,
                    AssignedEngineer = "Sarah Johnson",
                    PriorityId = mediumPriority?.PriorityId ?? 2,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(10),
                    TargetCompletionDate = DateTime.UtcNow.AddDays(8),
                    ShopId = avionicsShop?.ShopId ?? 2,
                    CreatedBy = teamLeader?.UserId ?? 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                },
                new CMT.Domain.Models.Task
                {
                    SerialNumber = "SN-ENG-003",
                    PartNumber = "PN-ENG-9876",
                    PoNumber = "PO-2024-003",
                    Description = "Fuel pump replacement and system check",
                    CategoryId = engineCategory?.CategoryId ?? 1,
                    RequestTypeId = repairType?.RequestTypeId ?? 2,
                    Status = CMT.Domain.Models.TaskStatus.Completed,
                    AssignedEngineer = "Mike Davis",
                    PriorityId = highPriority?.PriorityId ?? 3,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(-2),
                    TargetCompletionDate = DateTime.UtcNow.AddDays(-3),
                    ActualCompletionDate = DateTime.UtcNow.AddDays(-1),
                    ShopId = engineShop?.ShopId ?? 1,
                    CreatedBy = director?.UserId ?? 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new CMT.Domain.Models.Task
                {
                    SerialNumber = "SN-LG-004",
                    PartNumber = "PN-LG-5555",
                    PoNumber = "PO-2024-004",
                    Description = "Landing gear hydraulic actuator maintenance",
                    CategoryId = landingGearCategory?.CategoryId ?? 3,
                    RequestTypeId = maintenanceType?.RequestTypeId ?? 1,
                    Status = CMT.Domain.Models.TaskStatus.InProgress,
                    AssignedEngineer = "John Smith",
                    PriorityId = mediumPriority?.PriorityId ?? 2,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(14),
                    TargetCompletionDate = DateTime.UtcNow.AddDays(12),
                    ShopId = engineShop?.ShopId ?? 1,
                    CreatedBy = teamLeader?.UserId ?? 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow
                },
                new CMT.Domain.Models.Task
                {
                    SerialNumber = "SN-AVI-005",
                    PartNumber = "PN-AVI-7890",
                    PoNumber = "PO-2024-005",
                    Description = "Communication radio frequency alignment",
                    CategoryId = avionicsCategory?.CategoryId ?? 2,
                    RequestTypeId = repairType?.RequestTypeId ?? 2,
                    Status = CMT.Domain.Models.TaskStatus.Pending,
                    AssignedEngineer = "Sarah Johnson",
                    PriorityId = lowPriority?.PriorityId ?? 1,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(21),
                    TargetCompletionDate = DateTime.UtcNow.AddDays(18),
                    ShopId = avionicsShop?.ShopId ?? 2,
                    CreatedBy = director?.UserId ?? 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CMT.Domain.Models.Task
                {
                    SerialNumber = "SN-ENG-006",
                    PartNumber = "PN-ENG-1122",
                    PoNumber = "PO-2024-006",
                    Description = "Engine oil filter replacement and analysis",
                    CategoryId = engineCategory?.CategoryId ?? 1,
                    RequestTypeId = maintenanceType?.RequestTypeId ?? 1,
                    Status = CMT.Domain.Models.TaskStatus.InProgress,
                    AssignedEngineer = "Mike Davis",
                    PriorityId = mediumPriority?.PriorityId ?? 2,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(3),
                    TargetCompletionDate = DateTime.UtcNow.AddDays(2),
                    ShopId = engineShop?.ShopId ?? 1,
                    CreatedBy = teamLeader?.UserId ?? 2,
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Tasks.AddRangeAsync(tasks);
            await _context.SaveChangesAsync();
        }
    }
}