using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Data.Entities.UserEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class CargaMasivaTrabajadoresService : ICargaMasivaTrabajadoresService
    {
        private static readonly string[] CabecerasRequeridas =
        {
            "TipoRegistro",
            "Dni",
            "ApellidosNombres",
            "CorreoPersonal",
            "TelefonoPersonal",
            "IdSucursal",
            "DniJefe",
            "Cargo",
            "AreaDepartamento",
            "FechaIngreso",
            "IdEstado",
            "MarcajeEnZona",
            "Role"
        };

        private static readonly Regex DniRegex = new(@"^\d{8,15}$", RegexOptions.Compiled);

        private readonly MarcacionAsistenciaDbContext _context;
        private readonly ILogger<CargaMasivaTrabajadoresService> _logger;

        public CargaMasivaTrabajadoresService(
            MarcacionAsistenciaDbContext context,
            ILogger<CargaMasivaTrabajadoresService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CargaMasivaTrabajadoresResultadoDto> ProcesarCsvAsync(Stream streamCsv, string nombreArchivo, CancellationToken cancellationToken = default)
        {
            var importacionId = Guid.NewGuid().ToString("N");
            var fechaProceso = DateTime.UtcNow;

            using var reader = new StreamReader(streamCsv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var lineas = new List<string>();

            while (!reader.EndOfStream)
            {
                var linea = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(linea))
                {
                    lineas.Add(linea.Trim());
                }
            }

            if (lineas.Count == 0)
            {
                throw new InvalidOperationException("El archivo está vacío.");
            }

            var delimitador = DetectarDelimitador(lineas[0]);
            var cabeceras = ParsearLineaCsv(lineas[0], delimitador)
                .Select(v => v.Trim().Trim('\uFEFF'))
                .ToList();

            ValidarCabeceras(cabeceras);

            var indicePorCabecera = cabeceras
                .Select((cabecera, indice) => new { cabecera, indice })
                .ToDictionary(x => x.cabecera, x => x.indice, StringComparer.OrdinalIgnoreCase);

            var filasParseadas = new List<CargaMasivaTrabajadorFilaDto>();
            var resultados = new List<CargaMasivaFilaResultadoDto>();

            for (var numeroLinea = 2; numeroLinea <= lineas.Count; numeroLinea++)
            {
                var valores = ParsearLineaCsv(lineas[numeroLinea - 1], delimitador);
                if (valores.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                try
                {
                    var fila = MapearFila(valores, indicePorCabecera, numeroLinea);
                    filasParseadas.Add(fila);
                }
                catch (Exception ex)
                {
                    resultados.Add(new CargaMasivaFilaResultadoDto
                    {
                        NumeroFila = numeroLinea,
                        Estado = "Error",
                        Mensaje = ex.Message
                    });

                    _logger.LogWarning("Importación {ImportacionId}: fila {Fila} inválida. Error: {Error}", importacionId, numeroLinea, ex.Message);
                }
            }

            if (filasParseadas.Count == 0)
            {
                return ConstruirResultado(importacionId, nombreArchivo, fechaProceso, resultados);
            }

            var sucursalesValidasLista = await _context.SucursalCentros
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            var sucursalesValidas = sucursalesValidasLista.ToHashSet();

            var dniTrabajadorExistente = await _context.Trabajadores
                .Include(t => t.Persona)
                .Where(t => t.Persona != null)
                .ToDictionaryAsync(t => t.Persona.Dni, t => t.Id, cancellationToken);

            var dnisPersonaExistentesLista = await _context.Personas
                .Select(p => p.Dni)
                .ToListAsync(cancellationToken);
            var dnisPersonaExistentes = dnisPersonaExistentesLista.ToHashSet();

            var usernamesExistentesLista = await _context.Users
                .Select(u => u.Username)
                .ToListAsync(cancellationToken);
            var usernamesExistentes = usernamesExistentesLista.ToHashSet();

            var correosPersonalesExistentesLista = await _context.Personas
                .Where(p => p.CorreoPersonal != null)
                .Select(p => p.CorreoPersonal!)
                .ToListAsync(cancellationToken);
            var correosPersonalesExistentes = correosPersonalesExistentesLista.ToHashSet();

            var dnisDuplicadosEnArchivo = filasParseadas
                .GroupBy(f => f.Dni, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var filasConErrorPrevalidacion = new HashSet<int>();
            var dnisEnArchivo = filasParseadas
                .Select(f => f.Dni)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var fila in filasParseadas)
            {
                var errores = ValidarNegocioFila(fila, sucursalesValidas, dnisDuplicadosEnArchivo, dnisEnArchivo, dniTrabajadorExistente);
                if (errores.Count > 0)
                {
                    filasConErrorPrevalidacion.Add(fila.NumeroFila);
                    var mensajeError = string.Join(" | ", errores);
                    resultados.Add(new CargaMasivaFilaResultadoDto
                    {
                        NumeroFila = fila.NumeroFila,
                        Dni = fila.Dni,
                        Estado = "Error",
                        Mensaje = mensajeError
                    });

                    _logger.LogWarning("Importación {ImportacionId}: fila {Fila}, DNI {Dni}, error de validación: {Error}",
                        importacionId,
                        fila.NumeroFila,
                        fila.Dni,
                        mensajeError);
                }
            }

            var filasAProcesar = filasParseadas
                .Where(f => !filasConErrorPrevalidacion.Contains(f.NumeroFila))
                .OrderBy(f => f.TipoRegistro.Equals("JEFE", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(f => f.NumeroFila)
                .ToList();

            foreach (var fila in filasAProcesar)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (dnisPersonaExistentes.Contains(fila.Dni))
                {
                    AgregarErrorFila(resultados, fila, "Ya existe una persona registrada con ese DNI.", importacionId);
                    continue;
                }

                if (usernamesExistentes.Contains(fila.Dni))
                {
                    AgregarErrorFila(resultados, fila, "Ya existe un usuario registrado con ese username (DNI).", importacionId);
                    continue;
                }

                if (correosPersonalesExistentes.Contains(fila.CorreoPersonal))
                {
                    AgregarErrorFila(resultados, fila, "Ya existe una persona registrada con ese correo personal.", importacionId);
                    continue;
                }

                int? jefeInmediatoId = null;
                if (fila.TipoRegistro.Equals("TRABAJADOR", StringComparison.OrdinalIgnoreCase))
                {
                    var dniJefe = fila.DniJefe!.Trim();
                    if (!dniTrabajadorExistente.TryGetValue(dniJefe, out var idJefe))
                    {
                        AgregarErrorFila(resultados, fila, "No se pudo resolver el jefe por DNI. Verifica que el jefe exista y se haya cargado correctamente.", importacionId);
                        continue;
                    }

                    jefeInmediatoId = idJefe;
                }

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var user = new User
                    {
                        Username = fila.Dni,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(fila.Dni),
                        Email = fila.CorreoPersonal,
                        Role = fila.Role
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(cancellationToken);

                    var persona = new Persona
                    {
                        Dni = fila.Dni,
                        ApellidosNombres = fila.ApellidosNombres,
                        CorreoPersonal = fila.CorreoPersonal,
                        TelefonoPersonal = fila.TelefonoPersonal
                    };
                    _context.Personas.Add(persona);
                    await _context.SaveChangesAsync(cancellationToken);

                    var trabajador = new Trabajador
                    {
                        PersonaId = persona.Id,
                        JefeInmediatoId = jefeInmediatoId,
                        SucursalId = fila.IdSucursal,
                        UserId = user.Id,
                        Cargo = fila.Cargo,
                        AreaDepartamento = fila.AreaDepartamento,
                        FechaIngreso = fila.FechaIngreso,
                        IdEstado = fila.IdEstado,
                        MarcajeEnZona = fila.MarcajeEnZona
                    };

                    _context.Trabajadores.Add(trabajador);
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    dnisPersonaExistentes.Add(fila.Dni);
                    usernamesExistentes.Add(fila.Dni);
                    correosPersonalesExistentes.Add(fila.CorreoPersonal);
                    dniTrabajadorExistente[fila.Dni] = trabajador.Id;

                    resultados.Add(new CargaMasivaFilaResultadoDto
                    {
                        NumeroFila = fila.NumeroFila,
                        Dni = fila.Dni,
                        Estado = "Cargado",
                        Mensaje = "Registro creado correctamente.",
                        IdUserCreado = user.Id,
                        IdTrabajadorCreado = trabajador.Id
                    });

                    _logger.LogInformation("Importación {ImportacionId}: fila {Fila}, DNI {Dni}, trabajador {TrabajadorId} creado.",
                        importacionId,
                        fila.NumeroFila,
                        fila.Dni,
                        trabajador.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    AgregarErrorFila(resultados, fila, $"Error al guardar en base de datos: {ex.Message}", importacionId);
                }
            }

            return ConstruirResultado(importacionId, nombreArchivo, fechaProceso, resultados);
        }

        public async Task<CargaMasivaTrabajadoresResultadoDto> ProcesarXlsxAsync(Stream streamXlsx, string nombreArchivo, CancellationToken cancellationToken = default)
        {
            using var workbook = new XLWorkbook(streamXlsx);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new InvalidOperationException("El archivo XLSX no contiene hojas.");
            }

            var primeraFilaUsada = worksheet.FirstRowUsed();
            var ultimaFilaUsada = worksheet.LastRowUsed();
            if (primeraFilaUsada == null || ultimaFilaUsada == null)
            {
                throw new InvalidOperationException("La hoja de Excel está vacía.");
            }

            var primeraFilaNumero = primeraFilaUsada.RowNumber();
            var ultimaFilaNumero = ultimaFilaUsada.RowNumber();
            var ultimaColumnaUsada = worksheet.Row(primeraFilaNumero).LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (ultimaColumnaUsada == 0)
            {
                throw new InvalidOperationException("No se detectaron columnas en la hoja de Excel.");
            }

            var csvBuilder = new StringBuilder();

            for (var fila = primeraFilaNumero; fila <= ultimaFilaNumero; fila++)
            {
                var valores = new List<string>(ultimaColumnaUsada);
                for (var columna = 1; columna <= ultimaColumnaUsada; columna++)
                {
                    var texto = worksheet.Cell(fila, columna).GetFormattedString();
                    valores.Add(EscapeCsv(texto));
                }

                csvBuilder.AppendLine(string.Join(',', valores));
            }

            var contenidoCsv = csvBuilder.ToString();
            await using var streamCsv = new MemoryStream(Encoding.UTF8.GetBytes(contenidoCsv));

            return await ProcesarCsvAsync(streamCsv, nombreArchivo, cancellationToken);
        }

        private static CargaMasivaTrabajadoresResultadoDto ConstruirResultado(
            string importacionId,
            string nombreArchivo,
            DateTime fechaProceso,
            List<CargaMasivaFilaResultadoDto> resultados)
        {
            var totalCargadas = resultados.Count(r => r.Estado == "Cargado");
            var totalConError = resultados.Count(r => r.Estado == "Error");

            return new CargaMasivaTrabajadoresResultadoDto
            {
                ImportacionId = importacionId,
                NombreArchivo = nombreArchivo,
                FechaProcesoUtc = fechaProceso,
                TotalFilas = resultados.Count,
                TotalCargadas = totalCargadas,
                TotalConError = totalConError,
                DetalleFilas = resultados.OrderBy(r => r.NumeroFila).ToList()
            };
        }

        private void AgregarErrorFila(
            List<CargaMasivaFilaResultadoDto> resultados,
            CargaMasivaTrabajadorFilaDto fila,
            string mensaje,
            string importacionId)
        {
            resultados.Add(new CargaMasivaFilaResultadoDto
            {
                NumeroFila = fila.NumeroFila,
                Dni = fila.Dni,
                Estado = "Error",
                Mensaje = mensaje
            });

            _logger.LogWarning("Importación {ImportacionId}: fila {Fila}, DNI {Dni}, error: {Error}",
                importacionId,
                fila.NumeroFila,
                fila.Dni,
                mensaje);
        }

        private static List<string> ValidarNegocioFila(
            CargaMasivaTrabajadorFilaDto fila,
            HashSet<int> sucursalesValidas,
            HashSet<string> dnisDuplicadosEnArchivo,
            HashSet<string> dnisEnArchivo,
            Dictionary<string, int> dniTrabajadorExistente)
        {
            var errores = new List<string>();

            if (dnisDuplicadosEnArchivo.Contains(fila.Dni))
            {
                errores.Add("DNI duplicado dentro del mismo archivo.");
            }

            if (!sucursalesValidas.Contains(fila.IdSucursal))
            {
                errores.Add($"La sucursal con ID {fila.IdSucursal} no existe.");
            }

            if (fila.TipoRegistro.Equals("TRABAJADOR", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(fila.DniJefe))
                {
                    errores.Add("DniJefe es obligatorio para registros de tipo TRABAJADOR.");
                }
                else
                {
                    if (fila.DniJefe.Equals(fila.Dni, StringComparison.OrdinalIgnoreCase))
                    {
                        errores.Add("Un trabajador no puede ser su propio jefe.");
                    }

                    var existeEnArchivo = dnisEnArchivo.Contains(fila.DniJefe);
                    var existeEnBase = dniTrabajadorExistente.ContainsKey(fila.DniJefe);
                    if (!existeEnArchivo && !existeEnBase)
                    {
                        errores.Add("DniJefe no existe ni en la base de datos ni en el archivo de carga.");
                    }
                }
            }

            return errores;
        }

        private static CargaMasivaTrabajadorFilaDto MapearFila(
            List<string> valores,
            Dictionary<string, int> indicePorCabecera,
            int numeroFila)
        {
            string Obtener(string columna)
            {
                var indice = indicePorCabecera[columna];
                return indice < valores.Count ? valores[indice].Trim() : string.Empty;
            }

            var tipoRegistro = Obtener("TipoRegistro");
            var dni = Obtener("Dni");
            var apellidosNombres = Obtener("ApellidosNombres");
            var correoPersonal = Obtener("CorreoPersonal");
            var telefonoPersonal = Obtener("TelefonoPersonal");
            var idSucursalTexto = Obtener("IdSucursal");
            var dniJefe = Obtener("DniJefe");
            var cargo = Obtener("Cargo");
            var areaDepartamento = Obtener("AreaDepartamento");
            var fechaIngresoTexto = Obtener("FechaIngreso");
            var idEstadoTexto = Obtener("IdEstado");
            var marcajeEnZonaTexto = Obtener("MarcajeEnZona");
            var role = Obtener("Role");

            if (!tipoRegistro.Equals("JEFE", StringComparison.OrdinalIgnoreCase) &&
                !tipoRegistro.Equals("TRABAJADOR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("TipoRegistro debe ser JEFE o TRABAJADOR.");
            }

            if (!DniRegex.IsMatch(dni))
            {
                throw new InvalidOperationException("Dni debe tener entre 8 y 15 dígitos numéricos.");
            }

            if (string.IsNullOrWhiteSpace(apellidosNombres))
            {
                throw new InvalidOperationException("ApellidosNombres es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(correoPersonal))
            {
                throw new InvalidOperationException("CorreoPersonal es obligatorio.");
            }

            if (!EsCorreoValido(correoPersonal))
            {
                throw new InvalidOperationException("CorreoPersonal no tiene un formato válido.");
            }

            if (string.IsNullOrWhiteSpace(telefonoPersonal))
            {
                throw new InvalidOperationException("TelefonoPersonal es obligatorio.");
            }

            if (!int.TryParse(idSucursalTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idSucursal) || idSucursal <= 0)
            {
                throw new InvalidOperationException("IdSucursal debe ser un número entero mayor a 0.");
            }

            if (string.IsNullOrWhiteSpace(cargo))
            {
                throw new InvalidOperationException("Cargo es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(areaDepartamento))
            {
                throw new InvalidOperationException("AreaDepartamento es obligatorio.");
            }

            if (!DateTime.TryParseExact(fechaIngresoTexto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaIngreso))
            {
                throw new InvalidOperationException("FechaIngreso debe tener formato yyyy-MM-dd.");
            }

            if (!int.TryParse(idEstadoTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idEstado) || idEstado <= 0)
            {
                throw new InvalidOperationException("IdEstado debe ser un número entero mayor a 0.");
            }

            if (!TryParsearBooleanoFlexible(marcajeEnZonaTexto, out var marcajeEnZona))
            {
                throw new InvalidOperationException("MarcajeEnZona debe ser TRUE/FALSE, SI/NO o 1/0.");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                role = "Employee";
            }

            if (!role.Equals("Employee", StringComparison.OrdinalIgnoreCase) && !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Role solo admite Employee o Admin.");
            }

            return new CargaMasivaTrabajadorFilaDto
            {
                NumeroFila = numeroFila,
                TipoRegistro = tipoRegistro.ToUpperInvariant(),
                Dni = dni,
                ApellidosNombres = apellidosNombres,
                CorreoPersonal = correoPersonal,
                TelefonoPersonal = telefonoPersonal,
                IdSucursal = idSucursal,
                DniJefe = string.IsNullOrWhiteSpace(dniJefe) ? null : dniJefe,
                Cargo = cargo,
                AreaDepartamento = areaDepartamento,
                FechaIngreso = fechaIngreso,
                IdEstado = idEstado,
                MarcajeEnZona = marcajeEnZona,
                Role = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Employee"
            };
        }

        private static bool EsCorreoValido(string correo)
        {
            try
            {
                _ = new System.Net.Mail.MailAddress(correo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParsearBooleanoFlexible(string valor, out bool resultado)
        {
            var valorNormalizado = valor.Trim().ToUpperInvariant();

            switch (valorNormalizado)
            {
                case "TRUE":
                case "VERDADERO":
                case "SI":
                case "1":
                    resultado = true;
                    return true;

                case "FALSE":
                case "FALSO":
                case "NO":
                case "0":
                    resultado = false;
                    return true;

                default:
                    resultado = false;
                    return false;
            }
        }

        private static void ValidarCabeceras(List<string> cabeceras)
        {
            var faltantes = CabecerasRequeridas
                .Where(c => !cabeceras.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (faltantes.Count > 0)
            {
                throw new InvalidOperationException($"Faltan columnas requeridas: {string.Join(", ", faltantes)}");
            }
        }

        private static char DetectarDelimitador(string lineaCabecera)
        {
            var cantidadComas = lineaCabecera.Count(c => c == ',');
            var cantidadPuntoComa = lineaCabecera.Count(c => c == ';');

            return cantidadPuntoComa > cantidadComas ? ';' : ',';
        }

        private static List<string> ParsearLineaCsv(string linea, char delimitador)
        {
            var resultado = new List<string>();
            var acumulador = new StringBuilder();
            var enComillas = false;

            for (var indice = 0; indice < linea.Length; indice++)
            {
                var caracterActual = linea[indice];

                if (caracterActual == '"')
                {
                    if (enComillas && indice + 1 < linea.Length && linea[indice + 1] == '"')
                    {
                        acumulador.Append('"');
                        indice++;
                    }
                    else
                    {
                        enComillas = !enComillas;
                    }

                    continue;
                }

                if (caracterActual == delimitador && !enComillas)
                {
                    resultado.Add(acumulador.ToString());
                    acumulador.Clear();
                    continue;
                }

                acumulador.Append(caracterActual);
            }

            resultado.Add(acumulador.ToString());
            return resultado;
        }

        private static string EscapeCsv(string? valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return string.Empty;
            }

            var normalizado = valor.Replace("\r", " ").Replace("\n", " ").Trim();
            var requiereComillas = normalizado.Contains(',') || normalizado.Contains('"') || normalizado.Contains(';');
            if (!requiereComillas)
            {
                return normalizado;
            }

            return $"\"{normalizado.Replace("\"", "\"\"")}\"";
        }
    }
}
