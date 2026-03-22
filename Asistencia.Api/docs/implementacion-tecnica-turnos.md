Documentación Técnica: Implementación de Asignación de Turnos (FIJO vs ROTATIVO)

Esta documentación cubre todos los cambios implementados en el backend para soportar el flujo de asignación de horarios (fijo vs rotativo) según el diagrama especificado.

═══════════════════════════════════════════════════════════════════════════════
RESUMEN DE CAMBIOS IMPLEMENTADOS
═══════════════════════════════════════════════════════════════════════════════

1. DTOs extendidos: AsignacionTurnoCreateDto + AsignacionTurnoUpdateDto
   - Añadido: HorarioTurnoId, MotivoCambio, AprobadoPor
   - Soporte timestamps: CreatedAt, UpdatedAt

2. Entidad actualizada: AsignacionTurno
   - Nuevo campo: HorarioTurnoId (nullable) con relación a HorarioTurno
   - Relación: HasOne(a => a.HorarioTurno)

3. Validaciones en AsignacionTurnoService.AddAsync:
   - Si TipoTurno.NombreTipo contiene "ROT" → exige HorarioTurnoId
   - Valida que HorarioTurnoId exista y pertenezca al Turno
   - Detecta solapamientos de vigencias (lanza ArgumentException)

4. Validaciones en TrabajadoresController.AsignarTurnoTrabajador:
   - Paralelas al servicio (doble validación)
   - Devuelve 400 para rotativos sin horario
   - Devuelve 404 para horario inválido
   - Devuelve 409 Conflict para solapamientos

5. Nuevo servicio: IHorarioResolverService + HorarioResolverService
   - Resuelve HorarioDetalle por fecha y hora
   - Maneja SalidaDiaSiguiente (turnos nocturnos)
   - Busca por DiaSemana

6. Endpoint de programación semanal: ProgramacionSemanalController
   - GET /api/Rrhh/ProgramacionSemanal (lista por rango)
   - POST /api/Rrhh/ProgramacionSemanal/publicar (stub)
   - POST /api/Rrhh/ProgramacionSemanal/copiar (stub)

═══════════════════════════════════════════════════════════════════════════════
ENDPOINT 1: ASIGNAR TURNO (FIJO O ROTATIVO)
═══════════════════════════════════════════════════════════════════════════════

URL: POST /api/trabajadores/{trabajadorId}/asignar-turno
Rol requerido: ADMIN, SUPERADMIN, SUPERVISOR
Autenticación: Bearer JWT

REQUEST (JSON):
───────────────
{
  "turnoId": 5,
  "horarioTurnoId": 12,
  "fechaInicioVigencia": "2026-03-16T00:00:00Z",
  "fechaFinVigencia": "2026-06-16T00:00:00Z",
  "motivoCambio": "CAMBIO_TEMPORAL",
  "aprobadoPorTrabajadorId": 2
}

Campos explicados:
- turnoId (requerido): ID del turno a asignar
- horarioTurnoId (obligatorio para ROTATIVO, opcional para FIJO):
    Si el turno es ROTATIVO (TipoTurno.NombreTipo contiene "ROT"):
      → EXIGIDO. ID del HorarioTurno específico (patrón semanal)
    Si el turno es FIJO:
      → OPCIONAL. Si no se envía, el sistema usa primer horario activo
- fechaInicioVigencia (requerido): Fecha ISO 8601 inicio de vigencia
- fechaFinVigencia (opcional): Fecha ISO 8601 fin de vigencia (null = sin fin)
- motivoCambio (opcional): Código de motivo (ej. CAMBIO_TEMPORAL, ASCENSO, etc.)
- aprobadoPorTrabajadorId (opcional): ID del jefe que aprueba

EJEMPLO PRÁCTICO - TURNO FIJO:
──────────────────────────────
{
  "turnoId": 1,
  "horarioTurnoId": null,
  "fechaInicioVigencia": "2026-03-16T00:00:00Z",
  "fechaFinVigencia": null,
  "motivoCambio": "INGRESO",
  "aprobadoPorTrabajadorId": null
}
→ Asigna turno fijo al trabajador; sin fin de vigencia; aprobación por motivo INGRESO

EJEMPLO PRÁCTICO - TURNO ROTATIVO:
──────────────────────────────────
{
  "turnoId": 5,
  "horarioTurnoId": 12,
  "fechaInicioVigencia": "2026-03-16T00:00:00Z",
  "fechaFinVigencia": "2026-06-16T00:00:00Z",
  "motivoCambio": "CAMBIO_TEMPORAL",
  "aprobadoPorTrabajadorId": 2
}
→ Asigna turno rotativo con horario específico (patrón semanal ID 12) por 3 meses; aprobado por trabajador ID 2

RESPONSES:
──────────

201 CREATED - Éxito:
{
  "asignacionId": 456,
  "trabajadorId": 45,
  "turnoId": 5,
  "horarioTurnoId": 12,
  "fechaInicioVigencia": "2026-03-16",
  "fechaFinVigencia": "2026-06-16",
  "esVigente": true
}

400 BAD REQUEST - Turno rotativo sin horarioTurnoId:
{
  "message": "Para turnos rotativos se requiere HorarioTurnoId."
}

400 BAD REQUEST - Fechas inválidas:
{
  "message": "La fecha fin no puede ser menor que la fecha inicio."
}

404 NOT FOUND - Trabajador inexistente:
{
  "message": "No existe trabajador con ID {id}."
}

404 NOT FOUND - Turno inexistente:
{
  "message": "No existe turno con ID {request.TurnoId}."
}

404 NOT FOUND - HorarioTurno inexistente o no pertenece al turno:
{
  "message": "HorarioTurno con ID {horarioTurnoId} no encontrado o no pertenece al turno {turnoId}."
}

409 CONFLICT - Solapamiento de vigencias:
{
  "message": "La vigencia de la nueva asignación solapa con otra existente para el trabajador."
}

═══════════════════════════════════════════════════════════════════════════════
ENDPOINT 2: OBTENER TURNO VIGENTE COMPLETO
═══════════════════════════════════════════════════════════════════════════════

URL: GET /api/trabajadores/{trabajadorId}/turno-vigente
Rol requerido: Autenticado (cualquier rol)
Autenticación: Bearer JWT

PARÁMETROS:
───────────
{trabajadorId}: ID del trabajador (path parameter)

REQUEST (GET - sin body):
─────────────────────────
GET /api/trabajadores/45/turno-vigente
Authorization: Bearer <token>

RESPONSE 200 OK:
────────────────
{
  "trabajadorId": 45,
  "asignacionId": 123,
  "turno": {
    "id": 5,
    "codigo": "ROT-A",
    "tipoTurnoId": 2,
    "esActivo": true
  },
  "vigencia": {
    "inicio": "2026-03-16",
    "fin": "2026-06-16",
    "esVigente": true
  },
  "horario": {
    "idHorarioTurno": 12,
    "nombreHorario": "Semana A",
    "detalles": [
      {
        "diaSemana": "Monday",
        "horaInicio": "08:00",
        "horaFin": "16:00",
        "salidaDiaSiguiente": false
      },
      {
        "diaSemana": "Tuesday",
        "horaInicio": "08:00",
        "horaFin": "16:00",
        "salidaDiaSiguiente": false
      },
      ...
      {
        "diaSemana": "Sunday",
        "horaInicio": "23:00",
        "horaFin": "07:00",
        "salidaDiaSiguiente": true
      }
    ]
  }
}

Explicación de campos:
- trabajadorId: ID del trabajador consultado
- asignacionId: ID de la AsignacionTurno vigente
- turno.id: ID del Turno asignado
- turno.codigo: Nombre/código del turno (ej. ROT-A, FIJ-TARDE)
- turno.tipoTurnoId: ID del TipoTurno (1=FIJO, 2=ROTATIVO, etc.)
- turno.esActivo: Si el turno está activo en el sistema
- vigencia.inicio: Fecha de inicio de la asignación
- vigencia.fin: Fecha de fin (null = indefinido)
- vigencia.esVigente: Si la asignación está vigente (true = actualmente válida)
- horario.idHorarioTurno: ID del HorarioTurno asignado
- horario.nombreHorario: Nombre del patrón semanal (ej. "Semana A", "Patrón M/T/N")
- horario.detalles: Lista de HorarioDetalle por día de semana
  - diaSemana: Día semana ("Monday", "Tuesday", ..., "Sunday")
  - horaInicio: Hora inicio (formato HH:mm)
  - horaFin: Hora fin (formato HH:mm)
  - salidaDiaSiguiente: Si el turno termina al día siguiente (turnos nocturnos)

RESPONSE 404 NOT FOUND:
──────────────────────
{
  "message": "El trabajador no tiene turno vigente."
}

USO EN FRONTEND:
────────────────
1. Mostrar ficha del trabajador: usar turno.codigo + horario.nombreHorario
2. Mostrar patrón semanal: iterar horario.detalles y renderizar por diaSemana
3. Validación de marcación: comparar marca vs horario.detalles[día].horaInicio + tolerancia

═══════════════════════════════════════════════════════════════════════════════
ENDPOINT 3: LISTAR ASIGNACIONES (PAGINADO)
═══════════════════════════════════════════════════════════════════════════════

URL: GET /api/Rrhh/AsignacionTurno
Rol requerido: Autenticado
Autenticación: Bearer JWT
Paginación: Query string

REQUEST (GET):
──────────────
GET /api/Rrhh/AsignacionTurno?pageNumber=1&pageSize=20
Authorization: Bearer <token>

PARÁMETROS QUERY:
─────────────────
- pageNumber (default 1): Número de página
- pageSize (default 20): Registros por página

RESPONSE 200 OK:
────────────────
{
  "items": [
    {
      "id": 123,
      "trabajadorId": 45,
      "turnoId": 5,
      "horarioTurnoId": 12,
      "fechaInicioVigencia": "2026-03-16T00:00:00",
      "fechaFinVigencia": "2026-06-16T00:00:00",
      "esVigente": true,
      "motivoCambio": "CAMBIO_TEMPORAL",
      "aprobadoPor": 2,
      "createdAt": "2026-02-01T10:30:00Z",
      "updatedAt": "2026-02-15T14:20:00Z",
      "trabajador": {
        "id": 45,
        "persona": {
          "apellidosNombres": "Perez, Juan"
        }
      },
      "turno": {
        "id": 5,
        "nombreCodigo": "ROT-A",
        "tipoTurnoId": 2,
        "esActivo": true
      },
      "horarioTurno": {
        "id": 12,
        "nombreHorario": "Semana A",
        "esActivo": true
      }
    },
    ...más items
  ],
  "totalCount": 150,
  "pageSize": 20,
  "currentPage": 1,
  "totalPages": 8
}

Estructura PagedResult:
- items: Array de AsignacionTurno con datos expandidos
- totalCount: Total de registros en BD
- pageSize: Registros por página solicitado
- currentPage: Página actual
- totalPages: Total de páginas

═══════════════════════════════════════════════════════════════════════════════
ENDPOINT 4: PROGRAMACIÓN SEMANAL POR RANGO DE FECHAS
═══════════════════════════════════════════════════════════════════════════════

URL: GET /api/Rrhh/ProgramacionSemanal
Rol requerido: Autenticado
Autenticación: Bearer JWT

REQUEST (GET):
──────────────
GET /api/Rrhh/ProgramacionSemanal?fechaInicio=2026-03-16&fechaFin=2026-03-22
Authorization: Bearer <token>

PARÁMETROS QUERY:
─────────────────
- fechaInicio (requerido): Fecha inicio rango (formato YYYY-MM-DD)
- fechaFin (requerido): Fecha fin rango (formato YYYY-MM-DD)

RESPONSE 200 OK:
────────────────
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "items": [
    {
      "trabajadorId": 45,
      "trabajadorNombre": "Perez, Juan",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": 12,
          "horarioTurnoNombre": "Semana A",
          "turnoId": 5,
          "turnoNombre": "ROT-A",
          "estado": "planificado"
        },
        {
          "fecha": "2026-03-17",
          "horarioTurnoId": 12,
          "horarioTurnoNombre": "Semana A",
          "turnoId": 5,
          "turnoNombre": "ROT-A",
          "estado": "planificado"
        },
        {
          "fecha": "2026-03-18",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        ...
        {
          "fecha": "2026-03-22",
          "horarioTurnoId": 12,
          "horarioTurnoNombre": "Semana A",
          "turnoId": 5,
          "turnoNombre": "ROT-A",
          "estado": "planificado"
        }
      ]
    },
    {
      "trabajadorId": 46,
      "trabajadorNombre": "Garcia, Maria",
      "dias": [
        ...
      ]
    }
  ]
}

Estructura:
- fechaInicio/fechaFin: Rango consultado
- items: Array de trabajadores con su programación por fecha
  - trabajadorId: ID del trabajador
  - trabajadorNombre: Nombre completo
  - dias: Array de ProgramacionDiaDto (uno por cada fecha del rango)
    - fecha: Fecha específica
    - horarioTurnoId: ID del horario asignado (null si sin-asignar)
    - horarioTurnoNombre: Nombre del patrón (null si sin-asignar)
    - turnoId: ID del turno (null si sin-asignar)
    - turnoNombre: Código del turno (null si sin-asignar)
    - estado: "planificado" o "sin-asignar"

RESPONSE 400 BAD REQUEST:
────────────────────────
{
  "message": "fechaInicio no puede ser mayor a fechaFin"
}

USO EN FRONTEND:
────────────────
1. Navegar semanas: cambiar fechaInicio/fechaFin (+7 días cada vez)
2. Grilla visual: por cada trabajador + fecha → mostrar horarioTurnoNombre
3. Detectar gaps: filtrar by estado == "sin-asignar" para resaltar faltantes
4. NO depender de DiaSemana fijo; renderizar por fecha real

═══════════════════════════════════════════════════════════════════════════════
ENDPOINT 5: PUBLICAR SEMANA
═══════════════════════════════════════════════════════════════════════════════

URL: POST /api/Rrhh/ProgramacionSemanal/publicar
Rol requerido: ADMIN, SUPERADMIN
Autenticación: Bearer JWT

REQUEST (JSON):
───────────────
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "publicadoPorId": 2
}

Campos:
- fechaInicio (requerido): Inicio de semana a publicar
- fechaFin (requerido): Fin de semana a publicar
- publicadoPorId (requerido): ID del usuario que publica

RESPONSE 200 OK:
────────────────
{
  "publicadas": 7,
  "errores": []
}

O (si hay problemas):
{
  "publicadas": 5,
  "errores": [
    "Trabajador 45: sin asignación para 2026-03-18",
    "Trabajador 46: sin asignación para 2026-03-19"
  ]
}

RESPONSE 400 BAD REQUEST:
────────────────────────
{
  "message": "fechaInicio no puede ser mayor a fechaFin"
}

RESPUESTA 403 FORBIDDEN:
────────────────────────
(Automático si usuario no tiene rol ADMIN/SUPERADMIN)

USO EN FRONTEND:
────────────────
1. Botón "Publicar semana" solo visible para ADMIN/SUPERADMIN
2. Enviar fechas y usuarioId
3. Mostrar resumen de publicadas/errores

═══════════════════════════════════════════════════════════════════════════════
ENDPOINT 6: COPIAR SEMANA
═══════════════════════════════════════════════════════════════════════════════

URL: POST /api/Rrhh/ProgramacionSemanal/copiar
Rol requerido: ADMIN, SUPERADMIN
Autenticación: Bearer JWT

REQUEST (JSON):
───────────────
{
  "semanaOrigenInicio": "2026-03-09",
  "semanaDestinoInicio": "2026-03-16",
  "usuarioId": 2,
  "overwrite": false
}

Campos:
- semanaOrigenInicio (requerido): Primera fecha de semana origen (YYYY-MM-DD)
- semanaDestinoInicio (requerido): Primera fecha de semana destino (YYYY-MM-DD)
- usuarioId (requerido): ID del usuario que ejecuta la copia
- overwrite (opcional, default false): Si true, sobreescribe asignaciones existentes en destino

RESPONSE 200 OK:
────────────────
{
  "copiados": 42,
  "errores": []
}

O (con overwrite y conflictos):
{
  "copiados": 40,
  "errores": [
    "Trabajador 45 2026-03-16: ya tiene asignación (se ignoró; overwrite=false)"
  ]
}

RESPONSE 403 FORBIDDEN:
────────────────────────
(Automático si usuario no tiene rol ADMIN/SUPERADMIN)

USO EN FRONTEND:
────────────────
1. Diálogo "Copiar semana anterior"
2. Confirmar semana origen/destino
3. Opción checkbox "Overwrite" (por defecto false)
4. Enviar solicitud y mostrar resumen

═══════════════════════════════════════════════════════════════════════════════
SERVICIO: RESOLVER HORARIO DETALLE POR FECHA/HORA (INTERNO)
═══════════════════════════════════════════════════════════════════════════════

Servicio: IHorarioResolverService
Métodos:
  1. GetDetallesForDateAsync(int horarioTurnoId, DateTime date)
     → Retorna todos los HorarioDetalle para la fecha (por DiaSemana)
  
  2. ResolveDetalleForDateTimeAsync(int horarioTurnoId, DateTime dateTime)
     → Retorna el HorarioDetalle más específico para fecha + hora
     → Considera SalidaDiaSiguiente (turnos nocturnos)
     → Si múltiples bloques coinciden, devuelve el primero
     → Si ninguno coincide, devuelve el más cercano por horaInicio

EJEMPLO - RESOLVER TURNO NOCTURNO:
──────────────────────────────────
HorarioDetalle (del turno):
  - diaSemana: "Monday"
  - horaInicio: 23:00
  - horaFin: 07:00
  - salidaDiaSiguiente: true

Si le consultas por 2026-03-16T23:30:00 (lunes 23:30):
  → Devuelve ese detalle (está en la ventana 23:00-24:00)

Si le consultas por 2026-03-17T06:30:00 (martes 06:30):
  → Devuelve ese detalle (está en la ventana 00:00-07:00 del día siguiente)

═══════════════════════════════════════════════════════════════════════════════
VALIDACIONES AUTOMÁTICAS (FLUJO COMPLETO)
═══════════════════════════════════════════════════════════════════════════════

Cuando el admin llama POST /api/trabajadores/{id}/asignar-turno:

1. VALIDACIÓN DE EXISTENCIA:
   ✓ ¿Existe trabajador?
   ✓ ¿Existe turno?
   ✓ ✓ Carga TipoTurno del turno

2. VALIDACIÓN DE TIPO:
   ✓ ¿TipoTurno.NombreTipo contiene "ROT"?
     SI  → EXIGE horarioTurnoId; si no lo tiene → 400 Bad Request
     NO  → horarioTurnoId es opcional

3. VALIDACIÓN DE HORARIO (si se envía):
   ✓ ¿Existe HorarioTurno?
   ✓ ¿Pertenece al Turno?
   Si alguna falla → 404 Not Found

4. VALIDACIÓN DE VIGENCIA:
   ✓ ¿Hay solapamiento de fechas?
     SI  → 409 Conflict
     NO  → Continuar

5. SI TODO OK:
   ✓ Marca todas las asignaciones vigentes anteriores como NO vigentes
   ✓ Inserta nueva AsignacionTurno con HorarioTurnoId (si aplica)
   ✓ Guarda MotivoCambio, AprobadoPor, timestamps
   ✓ Retorna 201 Created

═══════════════════════════════════════════════════════════════════════════════
FLUJO COMPLETO: FIJO vs ROTATIVO
═══════════════════════════════════════════════════════════════════════════════

FLUJO FIJO:
──────────
1. Admin selecciona trabajador + turno FIJO
2. POST /api/trabajadores/45/asignar-turno
   {
     "turnoId": 1,
     "horarioTurnoId": null,  ← Opcional
     "fechaInicioVigencia": "2026-03-16",
     "fechaFinVigencia": null,
     "motivoCambio": "INGRESO"
   }
3. Backend:
   - Valida turno FIJO (no requiere horarioTurnoId)
   - Si no envía horarioTurnoId, se asigna el primer horario activo del turno
   - Inserta AsignacionTurno (horarioTurnoId = null o primer horario)
4. Frontend:
   - GET /api/trabajadores/45/turno-vigente
   - Renderiza horario semanal fijo
   - Validación diaria: compara marca vs horaInicio + tolerancia (mismo para todos los días)

FLUJO ROTATIVO:
───────────────
1. Admin selecciona trabajador + turno ROTATIVO
2. Admin elige patrón semanal (HorarioTurno) específico
3. POST /api/trabajadores/45/asignar-turno
   {
     "turnoId": 5,
     "horarioTurnoId": 12,  ← OBLIGATORIO
     "fechaInicioVigencia": "2026-03-16",
     "fechaFinVigencia": "2026-06-16",
     "motivoCambio": "CAMBIO_TEMPORAL"
   }
4. Backend:
   - Valida turno ROTATIVO (exige horarioTurnoId)
   - Valida que HorarioTurno 12 existe y pertenece a turno 5
   - Inserta AsignacionTurno con horarioTurnoId = 12
5. Frontend:
   - GET /api/trabajadores/45/turno-vigente
   - Renderiza horario semanal del patrón específico
   - Validación diaria:
     a) Resuelve HorarioDetalle para la fecha (por DiaSemana)
     b) Si múltiples bloques (M/T/N):
        - Consulta PROG_DESCANSOS (si existe) o
        - Usa heurística (horaInicio más cercana a hora marca)
     c) Compara marca vs horaInicio + tolerancia

INTEGRACIÓN CON CIERRE DIARIO:
───────────────────────────────
(Futuro - por implementar en CierreDiarioAsistenciaExecutor)

Para cada trabajador + fecha:
1. Obtén AsignacionTurno vigente → horarioTurnoId
2. Si horarioTurnoId != null:
   - Usa IHorarioResolverService.ResolveDetalleForDateTimeAsync()
   - Obtén horaInicio exacta para esa fecha/hora
3. Compara marcación vs horaInicio + tolerancia
4. Escribe ASISTENCIA_RESUMEN con estado ASISTENCIA/TARDANZA/FALTA/DESCANSO

═══════════════════════════════════════════════════════════════════════════════
MAPEO DE DTOs PARA FRONTEND (TypeScript)
═══════════════════════════════════════════════════════════════════════════════

Interface para AsignacionTurno (respuesta):
export interface AsignacionTurno {
  id: number;
  trabajadorId: number;
  turnoId: number;
  horarioTurnoId?: number;
  fechaInicioVigencia: string; // ISO date
  fechaFinVigencia?: string;   // ISO date
  esVigente: boolean;
  motivoCambio?: string;
  aprobadoPor?: number;
  createdAt: string;
  updatedAt?: string;
  trabajador?: {
    id: number;
    persona?: { apellidosNombres: string };
  };
  turno?: {
    id: number;
    nombreCodigo: string;
    tipoTurnoId: number;
    esActivo: boolean;
  };
  horarioTurno?: {
    id: number;
    nombreHorario: string;
    esActivo: boolean;
  };
}

Interface para AsignarTurnoRequest:
export interface AsignarTurnoRequest {
  turnoId: number;
  horarioTurnoId?: number;
  fechaInicioVigencia: string; // ISO date
  fechaFinVigencia?: string;   // ISO date
  motivoCambio?: string;
  aprobadoPorTrabajadorId?: number;
}

Interface para TurnoVigenteResponse:
export interface TurnoVigenteResponse {
  trabajadorId: number;
  asignacionId: number;
  turno: {
    id: number;
    codigo: string;
    tipoTurnoId: number;
    esActivo: boolean;
  };
  vigencia: {
    inicio: string;  // date
    fin?: string;    // date
    esVigente: boolean;
  };
  horario?: {
    idHorarioTurno: number;
    nombreHorario: string;
    detalles: Array<{
      diaSemana: string;
      horaInicio: string;   // HH:mm
      horaFin: string;      // HH:mm
      salidaDiaSiguiente: boolean;
    }>;
  };
}

═══════════════════════════════════════════════════════════════════════════════
CHECKLIST DE INTEGRACIÓN FRONTEND
═══════════════════════════════════════════════════════════════════════════════

En horarios.ts (o turno.ts):
  ☐ Cambiar logica "inferir ROT/FIJ por nombre" → usar TipoTurno.codigo (cuando esté implementado)
  ☐ Cuando asignar turno ROTATIVO → EXIGIR selección de horarioTurnoId
  ☐ Enviar horarioTurnoId en el body de asignar-turno
  ☐ Parsear respuesta para obtener horarioTurnoId
  ☐ No hardcodear motivoCambio → usar catálogo (si se implementa)

En turnos.ts (vista semanal):
  ☐ Ya NO iterar por diaSemana fijo
  ☐ Llamar GET /api/Rrhh/ProgramacionSemanal con rango dinámico
  ☐ Renderizar cada día real (no semana fija)
  ☐ Mostrar horarioTurnoId en cada celda (si rotativo)

En ficha trabajador:
  ☐ Llamar GET /api/trabajadores/{id}/turno-vigente
  ☐ Mostrar turnoNombre + horarioNombre
  ☐ Renderizar detalles[día].horaInicio/horaFin (no inferir por texto)

En botones de programación:
  ☐ "Publicar semana" → POST /api/Rrhh/ProgramacionSemanal/publicar
  ☐ "Copiar semana anterior" → POST /api/Rrhh/ProgramacionSemanal/copiar
  ☐ Validar permisos: usuario debe ser ADMIN/SUPERADMIN

═══════════════════════════════════════════════════════════════════════════════
NOTAS IMPORTANTES
═══════════════════════════════════════════════════════════════════════════════

1. DiaSemana: Asegúrate que en BD los valores sean "Monday", "Tuesday", ... "Sunday"
   (o equivalente en tu idioma, pero consistente en todas partes)

2. SalidaDiaSiguiente: Si está en true, la ventana de validación es [horaInicio, 24:00) ∪ [00:00, horaFin]

3. HorarioTurnoId es NULLABLE:
   - FIJO: generalmente null (usa primer horario activo del turno)
   - ROTATIVO: DEBE tener valor
   - Tanto ADD como UPDATE aceptan null

4. Solapamiento: Se valida estrictamente. Dos asignaciones no pueden solapar fechas.

5. EsVigente: Al crear nueva asignación, automáticamente marca anteriores como NO vigentes.

6. Timestamps: CreatedAt se asigna automático (SYSUTCDATETIME); UpdatedAt en actualizaciones.

7. Migraciones: Asegúrate de que el campo id_horario_turno existe en ASIGNACIONES_TURNO.

═══════════════════════════════════════════════════════════════════════════════
PRUEBAS MANUALES RECOMENDADAS
═══════════════════════════════════════════════════════════════════════════════

1. Asignar turno FIJO sin horarioTurnoId:
   POST /api/trabajadores/1/asignar-turno
   { turnoId: 1, fechaInicioVigencia: "2026-03-16" }
   → Debe aceptar (201 Created)

2. Asignar turno ROTATIVO sin horarioTurnoId:
   POST /api/trabajadores/1/asignar-turno
   { turnoId: 5, fechaInicioVigencia: "2026-03-16" }
   → Debe rechazar (400 Bad Request: "requiere HorarioTurnoId")

3. Asignar turno ROTATIVO con horarioTurnoId inválido:
   POST /api/trabajadores/1/asignar-turno
   { turnoId: 5, horarioTurnoId: 999, fechaInicioVigencia: "2026-03-16" }
   → Debe rechazar (404 Not Found: "no encontrado o no pertenece")

4. Asignar con solapamiento:
   (Primero asigna 2026-03-16 a 2026-03-22)
   (Luego intenta asignar 2026-03-20 a 2026-03-25)
   → Debe rechazar (409 Conflict: "solapa")

5. GET turno vigente:
   GET /api/trabajadores/1/turno-vigente
   → Debe devolver asignación con todos los detalles + horarioDetalle resuelto

6. GET programación semanal:
   GET /api/Rrhh/ProgramacionSemanal?fechaInicio=2026-03-16&fechaFin=2026-03-22
   → Debe devolver matriz trabajador × fecha con asignaciones

═══════════════════════════════════════════════════════════════════════════════
