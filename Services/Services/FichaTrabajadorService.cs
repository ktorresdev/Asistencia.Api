﻿using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class FichaTrabajadorService : IFichaTrabajadorService
    {

        private readonly MarcacionAsistenciaDbContext _context;

        public FichaTrabajadorService(MarcacionAsistenciaDbContext context) {
            _context = context;
        }
        public async Task<TrabajadorResponseDto> postCrearTrabajador(CreateTrabajadorDto createTrabajadorDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // --- 1. Validaciones Previas ---
                if (await _context.Personas.AnyAsync(p => p.Dni == createTrabajadorDto.Dni))
                {
                    throw new InvalidOperationException($"Ya existe una persona registrada con el DNI {createTrabajadorDto.Dni}.");
                }

                if (await _context.Personas.AnyAsync(p => p.CorreoPersonal == createTrabajadorDto.CorreoPersonal))
                {
                    throw new InvalidOperationException($"Ya existe una persona registrada con el correo {createTrabajadorDto.CorreoPersonal}.");
                }

                if (!await _context.SucursalCentros.AnyAsync(s => s.Id == createTrabajadorDto.IdSucursal))
                {
                    throw new KeyNotFoundException($"La sucursal con ID {createTrabajadorDto.IdSucursal} no fue encontrada.");
                }

                if (createTrabajadorDto.IdJefeInmediato.HasValue && !await _context.Trabajadores.AnyAsync(t => t.Id == createTrabajadorDto.IdJefeInmediato.Value))
                {
                    throw new KeyNotFoundException($"El jefe inmediato con ID {createTrabajadorDto.IdJefeInmediato.Value} no fue encontrado.");
                }

                var persona = new Persona()
                {
                    CorreoPersonal = createTrabajadorDto.CorreoPersonal,
                    ApellidosNombres = createTrabajadorDto.ApellidosNombres,
                    Dni = createTrabajadorDto.Dni,
                    TelefonoPersonal = createTrabajadorDto.TelefonoPersonal
                };

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                // --- 2. Crear el Trabajador ---
                var trabajador = new Trabajador()
                {
                    PersonaId = persona.Id,
                    JefeInmediatoId = createTrabajadorDto.IdJefeInmediato,
                    SucursalId = createTrabajadorDto.IdSucursal,
                    UserId = createTrabajadorDto.IdUser ?? 1,
                    Cargo = createTrabajadorDto.Cargo,
                    AreaDepartamento = createTrabajadorDto.AreaDepartamento,
                    FechaIngreso = createTrabajadorDto.FechaIngreso,
                    IdEstado = createTrabajadorDto.IdEstado
                };

                _context.Trabajadores.Add(trabajador);
                await _context.SaveChangesAsync();

                // --- 3. Confirmar la Transacción ---
                await transaction.CommitAsync();

                // Fetch state for response
                var estado = await _context.MaestroEstados.FindAsync(trabajador.IdEstado);

                return new TrabajadorResponseDto
                {
                    IdTrabajador = trabajador.Id,
                    Cargo = trabajador.Cargo,
                    AreaDepartamento = trabajador.AreaDepartamento,
                    IdEstado = trabajador.IdEstado,
                    Estado = estado!,
                    Persona = new Persona
                    {
                        Id = persona.Id,
                        Dni = persona.Dni,
                        ApellidosNombres = persona.ApellidosNombres
                    },
                    FechaIngreso = trabajador.FechaIngreso.GetValueOrDefault()
                };
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                // Lanza una excepción más amigable que la de EF Core
                throw new Exception("Error al guardar en la base de datos. Verifique que todos los IDs relacionados (Usuario, Sucursal, etc.) son correctos.", dbEx);
            }
            // El catch genérico no es necesario si ya capturas las excepciones específicas que esperas.
        }

        public async Task<TrabajadorResponseDto> UpdateTrabajadorAsync(int id, UpdateTrabajadorDto updateDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trabajador = await _context.Trabajadores
                    .Include(t => t.Persona)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (trabajador == null)
                {
                    throw new KeyNotFoundException($"No se encontró un trabajador con el ID {id}.");
                }

                // --- 1. Validaciones Previas ---
                if (await _context.Personas.AnyAsync(p => p.Dni == updateDto.Dni && p.Id != trabajador.PersonaId))
                {
                    throw new InvalidOperationException($"El DNI {updateDto.Dni} ya está registrado por otra persona.");
                }

                if (await _context.Personas.AnyAsync(p => p.CorreoPersonal == updateDto.CorreoPersonal && p.Id != trabajador.PersonaId))
                {
                    throw new InvalidOperationException($"El correo {updateDto.CorreoPersonal} ya está registrado por otra persona.");
                }

                if (!await _context.SucursalCentros.AnyAsync(s => s.Id == updateDto.IdSucursal))
                {
                    throw new KeyNotFoundException($"La sucursal no fue encontrada.");
                }

                if (updateDto.IdJefeInmediato.HasValue && !await _context.Trabajadores.AnyAsync(t => t.Id == updateDto.IdJefeInmediato.Value))
                {
                    throw new KeyNotFoundException($"El jefe inmediato no fue encontrado.");
                }

                // --- 2. Actualizar Entidades ---
                // Actualizar Persona
                trabajador.Persona.ApellidosNombres = updateDto.ApellidosNombres;
                trabajador.Persona.Dni = updateDto.Dni;
                trabajador.Persona.CorreoPersonal = updateDto.CorreoPersonal;
                trabajador.Persona.TelefonoPersonal = updateDto.TelefonoPersonal;

                // Actualizar Trabajador
                trabajador.SucursalId = updateDto.IdSucursal;
                trabajador.JefeInmediatoId = updateDto.IdJefeInmediato;
                trabajador.Cargo = updateDto.Cargo;
                trabajador.AreaDepartamento = updateDto.AreaDepartamento;
                trabajador.IdEstado = updateDto.IdEstado;

                _context.Trabajadores.Update(trabajador);
                await _context.SaveChangesAsync();

                // --- 3. Confirmar la Transacción ---
                await transaction.CommitAsync();

                return new TrabajadorResponseDto
                {
                    IdTrabajador = trabajador.Id,
                    Cargo = trabajador.Cargo,
                    AreaDepartamento = trabajador.AreaDepartamento,
                    IdEstado = trabajador.IdEstado,
                    Estado = trabajador.Estado!,
                    Persona = trabajador.Persona!,
                    FechaIngreso = trabajador.FechaIngreso.GetValueOrDefault()
                };
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error al actualizar en la base de datos. Verifique los datos e intente de nuevo.", dbEx);
            }
        }
    }
}
