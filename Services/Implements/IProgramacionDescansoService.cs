using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IProgramacionDescansoService
    {
        Task<IEnumerable<ProgramacionDescanso>> GetSemanaAsync(int idTrabajador, DateOnly fechaLunes);
        Task<ProgramacionDescanso> UpsertDiaAsync(int idTrabajador, DateOnly fecha, bool esDescanso, bool esDiaBoleta, int createdBy);
    }

    public class DescansoSemanaItemDto
    {
        public int IdTrabajador { get; set; }
        public int DiaDescanso { get; set; }
        public List<int> DiasBoleta { get; set; } = new List<int>();
    }
}
