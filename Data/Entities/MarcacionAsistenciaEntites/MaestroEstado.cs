namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class MaestroEstado
    {
        public int Id { get; set; }
        public required string GrupoEstado { get; set; }
        public required string NombreEstado { get; set; }
        public string? Descripcion { get; set; }
        public string? ColorHex { get; set; }
    }
}