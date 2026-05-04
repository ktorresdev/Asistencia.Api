using System;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class JustificacionDocumento
    {
        public int Id { get; set; }
        public int JustificacionId { get; set; }
        public string DocumentoUrl { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public virtual Justificacion Justificacion { get; set; } = null!;
    }
}
