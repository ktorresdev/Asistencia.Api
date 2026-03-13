using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.UserEntites
{
    [Table("DEVICE_TOKENS")]
    public class DeviceToken
    {
        [Key]
        [Column("id_device_token")]
        public int Id { get; set; }

        [Required]
        [Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("device_id")]
        [MaxLength(200)]
        public string? DeviceId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("revoked")]
        public bool Revoked { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
