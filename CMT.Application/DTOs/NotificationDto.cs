namespace CMT.Application.DTOs;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public int RecipientId { get; set; }
}