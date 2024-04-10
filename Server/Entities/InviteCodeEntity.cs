using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVidious.Data;

[Table("invite_code")]
public class InviteCodeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string Code { get; set; }
    public int RemainingUses { get; set; }
    public int UsageCount { get; set; }
}