using System;
using System.Collections.Generic;

namespace Asistencia.Services.Dtos
{
    public class ProgramacionSemanalResponseDto
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int TotalCount { get; set; }
        public List<ProgramacionPorTrabajadorDto> Items { get; set; } = new List<ProgramacionPorTrabajadorDto>();
    }

    public class ProgramacionPorTrabajadorDto
    {
        public int TrabajadorId { get; set; }
        public string? TrabajadorNombre { get; set; }
        public string? TipoTurnoNombre { get; set; }
        public int? TurnoId { get; set; }
        public int? SucursalId { get; set; }
        public List<ProgramacionDiaDto> Dias { get; set; } = new List<ProgramacionDiaDto>();
    }

    public class ProgramacionDiaDto
    {
        public DateOnly Fecha { get; set; }
        public int? HorarioTurnoId { get; set; }
        public string? HorarioTurnoNombre { get; set; }
        public int? TurnoId { get; set; }
        public string? TurnoNombre { get; set; }
        public string Estado { get; set; } = "planificado";
        public string? TipoAusencia { get; set; }
    }

    public class PublicarProgramacionDto
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
        public int PublicadoPorId { get; set; }
    }

    public class CopiarProgramacionDto
    {
        public DateOnly SemanaOrigenInicio { get; set; }
        public DateOnly SemanaDestinoInicio { get; set; }
        public int UsuarioId { get; set; }
        public bool Overwrite { get; set; }
    }
}
