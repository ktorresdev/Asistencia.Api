﻿using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IFichaTrabajadorService
    {
        Task<TrabajadorResponseDto> postCrearTrabajador(CreateTrabajadorDto createTrabajadorDto);
        Task<TrabajadorResponseDto> UpdateTrabajadorAsync(int id, UpdateTrabajadorDto updateDto);
    }
    public class TrabajadorResponseDto
    {
        public int IdTrabajador { get; set; }
        public required Persona Persona { get; set; }
        public required string Cargo { get; set; }
        public required string AreaDepartamento { get; set; }
        public long IdEstado { get; set; }
        public required MaestroEstado Estado { get; set; }
        public DateTime FechaIngreso { get; set; }
    }

    public class UpdateTrabajadorDto
    {
        // Campos de Persona
        public required string ApellidosNombres { get; set; }
        public required string Dni { get; set; }
        public required string CorreoPersonal { get; set; }
        public required string TelefonoPersonal { get; set; }

        // Campos de Trabajador
        public int IdSucursal { get; set; }
        public int? IdJefeInmediato { get; set; }
        public required string Cargo { get; set; }
        public required string AreaDepartamento { get; set; }
        public int IdEstado { get; set; }
    }
}
