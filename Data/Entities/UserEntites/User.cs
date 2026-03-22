using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Data.Entities.UserEntites
{
    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("id_user")]
        public int Id { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        [MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(200)]
        public string? Email { get; set; }

        [Required]
        [Column("role")]
        [MaxLength(20)]
        public string Role { get; set; } = "TRABAJADOR";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
