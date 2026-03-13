namespace Asistencia.Services.Dtos
{
    public class TurnoDto
    {
        public int id_turno { get; set; }
        public required string nombre_codigo { get; set; }
        public int tolerancia_ingreso { get; set; }
        public int tolerancia_salida { get; set; }
    }
}
