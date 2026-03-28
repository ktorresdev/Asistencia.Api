using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IReportesService
    {
        Task<List<ResponseResumenDto>> GetResumen(FiltroReporteDto filtro);
        Task<List<ResponseInasistenciasDto>> GetInasistencias(FiltroReporteDto filtro);
        Task<List<ResponseTardanzasDto>> GetTardanzas(FiltroReporteDto filtro);
        Task<List<ResponseHorasExtraDto>> GetHorasExtra(FiltroReporteDto filtro);

        //nuevos reportes
        Task<IEnumerable<ReporteInconsistenciaDto>> GetInconsistenciasAsync(string? area = null);
        Task<IEnumerable<ReporteAsistenciaDto>> GetResumenAsistenciaAsync(DateTime inicio, DateTime fin, string? dni, int? jefeId);

        Task<IEnumerable<ReporteTardanzaDto>> GetTardanzasAsync(DateTime inicio, DateTime fin, int? jefeId);
        Task<IEnumerable<ReporteHorasExtrasDto>> GetHorasExtrasAsync(DateTime inicio, DateTime fin, int? jefeId);
        Task<IEnumerable<ReporteTrabajadorJefeDto>> GetTrabajadoresPorJefeYFechaAsync(DateTime fecha, int jefeId, string? area = null);
        Task<IEnumerable<ReporteTrabajadorJefeDto>> GetTrabajadoresPorSucursalAsync(DateTime fecha, int sucursalId);

    }

    public class FiltroReporteDto
    {
        public int? IdTrabajador { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    public class ResponseResumenDto
    {
        public string? Dni { get; set; }
        public string? Nombre { get; set; }
        public string? CargoArea { get; set; }
        public int DiasTrabajados { get; set; }
        public int DiasDescanso { get; set; }
        public int DiasInasistencias { get; set; }
        public decimal HorasExtra { get; set; }
        public int DiasJustificados { get; set; }
    }

    public class ResponseInasistenciasDto
    {
        public DateTime? fechaSolicitud { get; set; }
        public int? trabajadorTipo { get; set; }
        public DateTime? fechaJustificada { get; set; }
        public string? motivoEstado { get; set; }
        public string? autorizadoPor { get; set; }
    }

    public class ResponseTardanzasDto
    {
        public DateTime fechaSolicitud { get; set; }
        public required string trabajadorCargo { get; set; }
        public int horasMotivo { get; set; }
        public required string estado { get; set; }
        public DateTime fecha { get; set; }
        public required string aprobación { get; set; }
    }

    public class ResponseHorasExtraDto
    {
        public DateTime Fecha { get; set; }
        public TimeSpan HoraSalidaProgramada { get; set; }
        public TimeSpan HoraSalidaMarcada { get; set; }
        public int MinutosExtra { get; set; }
    }

    //nuevos reportes 

    public class ReporteInconsistenciaDto
    {
        public required string DNI { get; set; }
        public required string Nombre { get; set; }
        public required string Cargo { get; set; }
        public required string Area { get; set; }
        public required string Fecha { get; set; } // Viene formateada DD/MM/YYYY desde SQL
        public required string Estado_Texto { get; set; }
        public required string Estado_Color_Code { get; set; }

        // Datos ocultos útiles para el frontend
        public long Id_Resumen { get; set; }
        public int Id_Trabajador { get; set; }
        public required string Tipo_Inconsistencia { get; set; }
    }

    public class ReporteAsistenciaDto
    {
        public int Id_Trabajador { get; set; }
        public required string DNI { get; set; }
        public required string Nombre { get; set; }
        public required string Cargo { get; set; }
        public required string Area { get; set; }

        // Métricas
        public int Dias_Programados_Laborables { get; set; }
        public int Dias_Trabajados { get; set; }
        public int Dias_Descanso_Feriados { get; set; }
        public int Inasistencias { get; set; }
        public int Dias_Tardanza { get; set; }
        public required string Tiempo_Tardanza { get; set; }
        public int Faltas { get; set; }     // Las rojas
        public required string Horas_Extra { get; set; }
        public int Justificados { get; set; }
    }

    public class ReporteTardanzaDto
    {
        public required string DNI { get; set; }
        public required string Nombre { get; set; }
        public required string Area { get; set; }
        public required string Fecha { get; set; }
        public required string Estado { get; set; }
        public required string Hora_Turno { get; set; }
        public required string Hora_Marcacion { get; set; }
        public int Minutos_Late { get; set; }
        public required string Tiempo_Tardanza_Texto { get; set; }
    }

    public class ReporteHorasExtrasDto
    {
        public required string DNI { get; set; }
        public required string Nombre { get; set; }
        public required string Area { get; set; }
        public required string Fecha { get; set; }
        public required string Salida_Turno { get; set; }
        public required string Salida_Real { get; set; }
        public required string Tiempo_Extra_Texto { get; set; }
        public int Total_Minutos_Extra { get; set; }
    }

    public class ReporteTrabajadorJefeDto
    {
        public int IdTrabajador { get; set; }
        public required string Nombre { get; set; }
        public required string Dni { get; set; }
        public string? Entrada { get; set; }
        public string? Salida { get; set; }
        public required string Estado { get; set; }
    }

}
