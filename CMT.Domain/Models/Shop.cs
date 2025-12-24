using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("shops")]
public class Shop : BaseEntity
{
    [Key]
    [Column("shop_id")]
    public int ShopId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description", TypeName = "TEXT")]
    public string? Description { get; set; }

    [Column("team_leader_id")]
    public int? TeamLeaderId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey("TeamLeaderId")]
    public virtual User? TeamLeader { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}