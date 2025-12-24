using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SystemTask = System.Threading.Tasks.Task;

namespace CMT.Web.Api.Hubs;

[AllowAnonymous] // Temporarily allow anonymous access for testing
public class NotificationHub : Hub
{
    public async SystemTask JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        Console.WriteLine($"[SIGNALR] User {userId} joined group User_{userId} with connection {Context.ConnectionId}");
    }

    public async SystemTask LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        Console.WriteLine($"[SIGNALR] User {userId} left group User_{userId} with connection {Context.ConnectionId}");
    }

    public override async SystemTask OnConnectedAsync()
    {
        var userId = GetUserIdFromContext();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            Console.WriteLine($"[SIGNALR] User {userId} connected with connection {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"[SIGNALR] Anonymous user connected with connection {Context.ConnectionId}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async SystemTask OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdFromContext();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            Console.WriteLine($"[SIGNALR] User {userId} disconnected from connection {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"[SIGNALR] Anonymous user disconnected from connection {Context.ConnectionId}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    private string? GetUserIdFromContext()
    {
        // Try multiple claim types for compatibility
        var userId = Context.User?.FindFirst("user_id")?.Value ??
                     Context.User?.FindFirst("userId")?.Value ??
                     Context.User?.FindFirst("id")?.Value ??
                     Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return userId;
    }
}