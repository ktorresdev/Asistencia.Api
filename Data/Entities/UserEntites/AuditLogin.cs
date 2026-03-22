using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Data.Entities.UserEntites
{
    public class AuditLogin
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string UsernameIntentado { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string Resultado { get; set; } = string.Empty; // OK | FAIL | BLOCKED
        public string? MotivoFallo { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}
