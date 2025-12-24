using CMT.Application.Interfaces;
using CMT.Domain.Models;
using CMT.Domain.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskEntity = CMT.Domain.Models.Task;

namespace CMT.Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<UserRole, List<string>> _rolePermissions;

        public AuthorizationService(ApplicationDbContext context)
        {
            _context = context;
            _rolePermissions = InitializeRolePermissions();
        }

        private Dictionary<UserRole, List<string>> InitializeRolePermissions()
        {
            return new Dictionary<UserRole, List<string>>
            {
                [UserRole.Director] = new List<string>
                {
                    // Full system access - both old and new permission names
                    "create_task", "view_all_tasks", "update_task_status",
                    "delete_task", "assign_task", "complete_task",
                    "request_amendment", "approve_amendment",
                    "create_user", "view_all_users", "update_user",
                    "delete_user", "manage_roles",
                    "manage_categories", "manage_priorities",
                    "view_reports", "export_data", "system_settings",
                    "view_all_performance",
                    // New permission names used by controllers
                    "ViewTasks", "CreateTasks", "EditTasks", "DeleteTasks"
                },
                [UserRole.TeamLeader] = new List<string>
                {
                    // Team management and task oversight
                    "create_task", "view_all_tasks", "update_task_status",
                    "delete_task", "assign_task", "complete_task",
                    "request_amendment",
                    "create_user", "view_all_users", "update_user",
                    "delete_user", "manage_roles",
                    "manage_categories", "manage_priorities",
                    "view_reports", "export_data",
                    "view_team_performance",
                    // New permission names used by controllers
                    "ViewTasks", "CreateTasks", "EditTasks", "DeleteTasks"
                },
                [UserRole.ShopTL] = new List<string>
                {
                    // Shop-specific management
                    "create_task", "view_shop_tasks", "update_task_status",
                    "assign_task", "complete_task", "request_amendment",
                    "manage_shop_users", "view_team_performance",
                    // New permission names used by controllers
                    "ViewTasks", "CreateTasks", "EditTasks"
                },
                [UserRole.Engineer] = new List<string>
                {
                    // Task execution and personal management
                    "view_own_tasks", "update_task_status",
                    "request_amendment", "view_performance",
                    // New permission names used by controllers
                    "ViewTasks", "EditTasks"
                },
                [UserRole.CustomerPersonnel] = new List<string>
                {
                    // Department task creation and tracking
                    "create_task", "view_own_tasks",
                    "update_task_status", "request_amendment",
                    // New permission names used by controllers
                    "ViewTasks", "CreateTasks", "EditTasks"
                },
                [UserRole.Customer] = new List<string>
                {
                    // Basic task creation and viewing
                    "create_task", "view_own_tasks",
                    // New permission names used by controllers
                    "ViewTasks", "CreateTasks"
                }
            };
        }

        public async System.Threading.Tasks.Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            var userId = GetUserIdFromClaims(user);
            if (userId == null) return false;

            return await HasPermissionAsync(userId.Value, permission);
        }

        public async System.Threading.Tasks.Task<bool> HasPermissionAsync(int userId, string permission)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status != UserStatus.Active) return false;

            var rolePermissions = _rolePermissions.GetValueOrDefault(user.Role, new List<string>());
            return rolePermissions.Contains(permission);
        }

        public async System.Threading.Tasks.Task<bool> HasRoleAsync(ClaimsPrincipal user, CMT.Application.DTOs.UserRole role)
        {
            var userId = GetUserIdFromClaims(user);
            if (userId == null) return false;

            return await HasRoleAsync(userId.Value, role);
        }

        public async System.Threading.Tasks.Task<bool> HasRoleAsync(int userId, CMT.Application.DTOs.UserRole role)
        {
            var user = await _context.Users.FindAsync(userId);
            return user != null && (int)user.Role == (int)role && user.Status == UserStatus.Active;
        }

        public async System.Threading.Tasks.Task<bool> CanAccessTaskAsync(ClaimsPrincipal user, int taskId)
        {
            var userId = GetUserIdFromClaims(user);
            if (userId == null) return false;

            var userEntity = await _context.Users.FindAsync(userId.Value);
            if (userEntity == null || userEntity.Status != UserStatus.Active) return false;

            var task = await _context.Tasks
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (task == null) return false;

            // Directors and Team Leaders can access all tasks
            if (userEntity.Role == UserRole.Director || userEntity.Role == UserRole.TeamLeader)
                return true;

            // Shop Team Leaders can access tasks in their shop
            if (userEntity.Role == UserRole.ShopTL && userEntity.ShopId.HasValue)
            {
                var taskInShop = task.Creator?.ShopId == userEntity.ShopId;
                if (taskInShop) return true;
            }

            // Users can access tasks they created
            return task.CreatedBy == userId.Value;
        }

        public async System.Threading.Tasks.Task<bool> CanManageUserAsync(ClaimsPrincipal user, int targetUserId)
        {
            var userId = GetUserIdFromClaims(user);
            if (userId == null) return false;

            var currentUser = await _context.Users.FindAsync(userId.Value);
            var targetUser = await _context.Users.FindAsync(targetUserId);

            if (currentUser == null || targetUser == null || currentUser.Status != UserStatus.Active) return false;

            // Users cannot manage themselves through this method
            if (userId.Value == targetUserId) return false;

            // Directors can manage all users
            if (currentUser.Role == UserRole.Director) return true;

            // Team Leaders can manage users below their level
            if (currentUser.Role == UserRole.TeamLeader && 
                IsHigherRole((CMT.Application.DTOs.UserRole)currentUser.Role, (CMT.Application.DTOs.UserRole)targetUser.Role)) return true;

            // Shop Team Leaders can manage users in their shop
            if (currentUser.Role == UserRole.ShopTL && 
                currentUser.ShopId.HasValue && 
                targetUser.ShopId == currentUser.ShopId &&
                IsHigherRole((CMT.Application.DTOs.UserRole)currentUser.Role, (CMT.Application.DTOs.UserRole)targetUser.Role)) return true;

            return false;
        }

        public async System.Threading.Tasks.Task<bool> IsInSameShopAsync(int userId1, int userId2)
        {
            var user1 = await _context.Users.FindAsync(userId1);
            var user2 = await _context.Users.FindAsync(userId2);

            return user1?.ShopId != null && user1.ShopId == user2?.ShopId;
        }

        public async System.Threading.Tasks.Task<bool> IsSupervisorOfAsync(int supervisorId, int subordinateId)
        {
            var subordinate = await _context.Users.FindAsync(subordinateId);
            return subordinate?.SupervisorId == supervisorId;
        }

        public async System.Threading.Tasks.Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status != UserStatus.Active) return new List<string>();

            return _rolePermissions.GetValueOrDefault(user.Role, new List<string>());
        }

        public async System.Threading.Tasks.Task<List<string>> GetRolePermissionsAsync(CMT.Application.DTOs.UserRole role)
        {
            await System.Threading.Tasks.Task.CompletedTask; // For async consistency
            var domainRole = (UserRole)role;
            return _rolePermissions.GetValueOrDefault(domainRole, new List<string>());
        }

        public bool IsHigherRole(CMT.Application.DTOs.UserRole role1, CMT.Application.DTOs.UserRole role2)
        {
            var roleHierarchy = new Dictionary<CMT.Application.DTOs.UserRole, int>
            {
                [CMT.Application.DTOs.UserRole.Customer] = 1,
                [CMT.Application.DTOs.UserRole.CustomerPersonnel] = 2,
                [CMT.Application.DTOs.UserRole.Engineer] = 3,
                [CMT.Application.DTOs.UserRole.ShopTeamLeader] = 4,
                [CMT.Application.DTOs.UserRole.TeamLeader] = 5,
                [CMT.Application.DTOs.UserRole.Director] = 6
            };

            return roleHierarchy.GetValueOrDefault(role1, 0) > roleHierarchy.GetValueOrDefault(role2, 0);
        }

        public async System.Threading.Tasks.Task<bool> CanViewPerformanceAsync(ClaimsPrincipal user, int? targetUserId = null)
        {
            var userId = GetUserIdFromClaims(user);
            if (userId == null) return false;

            var currentUser = await _context.Users.FindAsync(userId.Value);
            if (currentUser == null || currentUser.Status != UserStatus.Active) return false;

            // Directors can view all performance data
            if (currentUser.Role == UserRole.Director) return true;

            // Team Leaders can view team performance
            if (currentUser.Role == UserRole.TeamLeader) return true;

            // Shop Team Leaders can view their shop's performance
            if (currentUser.Role == UserRole.ShopTL) return true;

            // Engineers can view their own performance
            if (currentUser.Role == UserRole.Engineer)
            {
                return targetUserId == null || targetUserId == userId.Value;
            }

            return false;
        }

        public async System.Threading.Tasks.Task<bool> CanAccessShopAsync(ClaimsPrincipal user, int shopId)
        {
            var userId = GetUserIdFromClaims(user);
            if (userId == null) return false;

            var currentUser = await _context.Users.FindAsync(userId.Value);
            if (currentUser == null || currentUser.Status != UserStatus.Active) return false;

            // Directors and Team Leaders can access all shops
            if (currentUser.Role == UserRole.Director || currentUser.Role == UserRole.TeamLeader)
                return true;

            // Shop Team Leaders can only access their assigned shop
            if (currentUser.Role == UserRole.ShopTL)
                return currentUser.ShopId == shopId;

            return false;
        }

        private int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}