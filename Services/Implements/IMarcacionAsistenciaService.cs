using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IMarcacionAsistenciaService
    {
        Task<PagedResult<MarcacionAsistencia>> GetAllAsync(PaginationDto pagination);
        Task<MarcacionResponse> AddMarcacionAsync(MarcacionRequest marcacionRequest);
        Task<MarcacionAsistencia> getMarcadoAsistenciaAsync(int trabajadorId);
        Task<TimeWorkedDto> CalculateTimeWorkedAsync(int trabajadorId);
    }

    public class MarcacionRequest
    {
        [Required]
        public int IdTrabajador { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string? FotoUrl { get; set; }
    }

    public class MarcacionResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public string? Code { get; set; }
        public string? Detail { get; set; }
        public MarcacionAsistencia? Data { get; set; }
    }

    public class TimeWorkedDto
    {
        public required string ScheduledTime { get; set; }
        public double TimeWorkedMinutes { get; set; }
        public required string TimeWorkedFormatted { get; set; }
        public DateTime? EntryRegisteredAt { get; set; }
        public DateTime? ExitRegisteredAt { get; set; }
        public required string StatusMessage { get; set; }
    }
}
