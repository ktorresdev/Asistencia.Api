# Flujo completo: Marcación, Coberturas y Justificaciones
**Proyecto:** Asistencia.Api — Energigas  
**Actualizado:** 2026-04-24

---

## 1. Flujo de Marcación

### 1.1 Resumen del flujo

```
App Flutter
    │
    ├─ GET /api/Rrhh/MarcacionAsistencia/status/{trabajadorId}
    │       └─ Consulta estado actual: ¿puede marcar entrada o salida?
    │
    └─ POST /api/Rrhh/MarcacionAsistencia
            ├─ JSON:        { idTrabajador, latitud, longitud, fotoUrl? }
            └─ multipart:   { idTrabajador, latitud, longitud, foto (archivo) }
```

### 1.2 Pasos internos de `AddMarcacionAsync`

#### Paso 1 — Resolver contexto de horario (`ResolveShiftContextAsync`)

Busca el turno vigente para el trabajador en el momento de la marcación.  
Prioridad de fuente:

| Prioridad | Fuente | Descripción |
|---|---|---|
| 1 | `PTS` | `PROGRAMACION_TURNOS_SEMANAL` — programación explícita del día (rotativos) |
| 2 | `PTS_FALLBACK` | PTS existe pero hora fuera de ventana, se usa como base |
| 3 | `ASIGNACION_BASE` | `ASIGNACIONES_TURNO` — turno fijo o rotativo sin PTS cargada |
| 4 | `ASIGNACION_FALLBACK` | Asignación existe pero fuera de ventana |
| 5 | `DEFAULT` | Horario genérico 08:00-18:30 cuando no hay otra configuración |
| — | `SIN_ASIGNACION` | El trabajador no tiene ningún turno asignado → error |

**Doble turno:** Cuando varios `HorarioDetalle` hacen match simultáneo (ej. turno 19:00-00:00 y turno 00:00-06:00 solapan ventana a las 00:05), se aplica la siguiente prioridad:
1. Turno cuyo `ScheduledEnd >= ahora` (aún activo)
2. Entre activos, el que inició más recientemente (`max ScheduledStart`)

**Ventana de marcación:** ±2 horas alrededor del turno programado.  
Ejemplo: turno 07:00-15:00 → ventana 05:00-17:00.

**Turnos nocturnos:** Se prueba el día actual, el día anterior (marcas nocturnas después de medianoche) y el día siguiente (entradas muy anticipadas). La fecha base exacta del match se propaga para evitar errores al calcular turnos que terminan exactamente a las 00:00.

#### Paso 2 — Cargar trabajador y sucursal

- Se cargan el trabajador y sus sedes asignadas (principal + `TRABAJADOR_SUCURSALES` vigentes).
- Si no tiene ninguna sede → error `ERROR_SIN_SEDE`.

#### Paso 3 — Validar geolocalización

- Se evalúa en cuál sede se encuentra el trabajador por GPS (fórmula de Haversine).
- Si está dentro del geofence de alguna sede → se usa la más cercana de esas.
- Si está fuera de todas y el trabajador tiene `MarcajeEnZona = true` → error `ERROR_FUERA_ZONA`.
- La sede donde marcó físicamente (`SucursalMarcacionId`) se guarda separado de la sede de pertenencia (`SucursalId`) para auditoría.

#### Paso 4 — Determinar tipo de marcación (ENTRADA / SALIDA)

Se consultan las marcaciones existentes dentro de la ventana del turno resuelto:

```
Sin marcas → ENTRADA
Última marca = ENTRADA → SALIDA
Última marca = SALIDA → ERROR_SALIDA_REGISTRADA
```

**Soporte doble turno:** Antes de evaluar, se filtran las marcaciones de `SALIDA` cuya `FechaHora < ScheduledStart` del turno actual. Esto permite que la SALIDA del primer turno (ej. 23:58) no bloquee la ENTRADA del segundo turno (ej. 00:05 del día siguiente).

#### Paso 5 — Evitar duplicados recientes

Si ya existe una marcación del mismo tipo dentro de los últimos 120 segundos → error `ERROR_DUPLICADO_RECIENTE`.

#### Paso 6 — Guardar marcación

```
MARCACIONES_ASISTENCIA
  ├─ TrabajadorId
  ├─ FechaHora (DateTime.Now)
  ├─ TipoMarcacion: "ENTRADA" | "SALIDA"
  ├─ Latitud / Longitud
  ├─ FotoUrl (si se subió imagen)
  ├─ UbicacionValida (resultado del geofence)
  ├─ SucursalId → sede de pertenencia (para reportes)
  └─ SucursalMarcacionId → sede física donde se marcó (auditoría)
```

### 1.3 Endpoint de estado (GET status)

`GET /api/Rrhh/MarcacionAsistencia/status/{trabajadorId}`

Llama a `CalculateTimeWorkedAsync` y devuelve:

| Campo | Descripción |
|---|---|
| `horarioProgramado` | Rango HH:mm - HH:mm del turno resuelto |
| `marcacionEntrada` | DateTime de la entrada registrada (null si no hay) |
| `marcacionSalida` | DateTime de la salida registrada (null si no hay) |
| `tiempoTrabajadoMinutos` | Minutos trabajados (si aún no salió, cuenta hasta ahora) |
| `tiempoTrabajadoFormato` | "Xh Ym" |
| `puedeMarcarEntrada` | `true` si aún no registró entrada |
| `puedeMarcarSalida` | `true` si entró pero aún no salió |
| `salidaPendiente` | `true` si tiene una entrada sin cierre |

### 1.4 Códigos de error de marcación

| Código | HTTP | Descripción |
|---|---|---|
| `ERROR_NO_TURNO` | 404 | El trabajador no tiene turno asignado |
| `ERROR_TRABAJADOR_NO_ENCONTRADO` | 404 | Id de trabajador inválido |
| `ERROR_SIN_HORARIO` | 404 | Turno asignado pero sin detalles de horario |
| `ERROR_SIN_SEDE` | 400 | Trabajador sin ninguna sede configurada |
| `ERROR_FUERA_ZONA` | 403 | GPS fuera del geofence de todas sus sedes |
| `ERROR_SALIDA_REGISTRADA` | 409 | Ya completó el turno (entrada + salida) |
| `ERROR_DUPLICADO_RECIENTE` | 409 | Marca duplicada en < 120 segundos |
| `SUCCESS_MARCACION_OK` | 201 | Marcación registrada correctamente |

### 1.5 Imágenes de marcación

- Soporta `multipart/form-data` con campo `foto` (JPEG, PNG, WEBP, máx. 3 MB).
- Se guarda en `wwwroot/uploads/marcaciones/yyyy/MM/{guid}.ext`.
- La URL absoluta se almacena en `FotoUrl`.

---

## 2. Cierre Diario de Asistencia

### 2.1 Job automático

`CierreDiarioAsistenciaJob` es un `BackgroundService` que se ejecuta **cada 3 minutos** mientras la API esté corriendo.  
Llama al stored procedure:

```sql
EXEC dbo.SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA '{fecha}'
```

El SP consolida las marcaciones del día en `ASISTENCIA_RESUMEN_DIARIO`, calculando:
- Estado de asistencia (PRESENTE, TARDANZA, FALTA, etc.)
- `MinutosTardanza`
- `MinutosExtra`
- Hora de entrada y salida teórica vs real

### 2.2 Ejecución manual

`POST /api/Rrhh/CierreDiarioAsistencia/ejecutar/{yyyy-MM-dd}`  
Requiere autenticación. Útil para reprocesar un día específico.

### 2.3 Consulta de resumen

`GET /api/Asistencia/resumen?fecha=yyyy-MM-dd`

- SUPERADMIN → ve todos los trabajadores.
- ADMIN/SUPERVISOR → ve solo los trabajadores cuyo `id_jefe_inmediato` coincide con el suyo.

---

## 3. Coberturas

### 3.1 ¿Qué es una cobertura?

Registro de que un trabajador cubre el turno de otro (ausente). Tipos:

| TipoCobertura | Descripción |
|---|---|
| `COBERTURA` | Reemplazo simple, sin devolución |
| `CAMBIO` | Intercambio de turno entre dos trabajadores |
| `ANTICIPO` | El trabajador cubre antes de que le toque |

Un `CAMBIO` puede generar un registro recíproco (`IdCoberturaReciproca`).

### 3.2 Flujo de registro

```
POST /api/Coberturas
  Body:
    fecha                     → fecha del turno a cubrir
    idTrabajadorCubre         → quien va a trabajar
    idTrabajadorAusente       → quien faltará (null si es solo asignación)
    idHorarioTurnoOriginal    → turno original del ausente
    tipoCobertura             → "COBERTURA" | "CAMBIO" | "ANTICIPO"
    fechaSwapDevolucion       → (solo CAMBIO) fecha en que se devuelve el favor
    aprobadoPor               → id_trabajador del jefe que aprueba
    esSoloAsignacion          → true = no hay ausente, es solo asignación extra
```

Internamente llama al stored procedure:

```sql
EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO
    @fecha, @id_trabajador_cubre, @id_trabajador_ausente,
    @id_horario_turno_original, @tipo_cobertura,
    @fecha_swap_devolucion, @aprobado_por
```

El SP valida reglas de negocio y lanza `RAISERROR` / `THROW` si hay conflictos  
(el API lo captura como `SqlException` y devuelve 400 con el mensaje del SP).

### 3.3 Estados de una cobertura

```
PENDIENTE → APROBADO
          → RECHAZADO
```

### 3.4 Endpoints de coberturas

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| `POST` | `/api/Coberturas` | ADMIN, SUPERADMIN, SUPERVISOR | Registrar cobertura |
| `GET` | `/api/Coberturas` | Todos | Listar (filtrado por rol) |
| `PUT` | `/api/Coberturas/{id}/aprobar` | ADMIN, SUPERADMIN, SUPERVISOR | Aprobar |
| `PUT` | `/api/Coberturas/{id}/rechazar` | ADMIN, SUPERADMIN, SUPERVISOR | Rechazar |

### 3.5 Visibilidad por rol en GET

| Rol | Qué ve |
|---|---|
| `SUPERADMIN` | Todas las coberturas |
| `ADMIN` / `SUPERVISOR` | Solo coberturas donde el ausente o el que cubre son sus subordinados (`id_jefe_inmediato`) |
| `TRABAJADOR` | Solo sus propias coberturas (como ausente o como quien cubre) |

### 3.6 Filtros disponibles en GET

- `?fecha=yyyy-MM-dd` — filtrar por fecha
- `?estado=PENDIENTE|APROBADO|RECHAZADO` — filtrar por estado
- `?idTrabajador={id}` — filtrar por trabajador específico

---

## 4. Justificaciones

### 4.1 Estado actual

La entidad y las tablas existen en base de datos (`JUSTIFICACIONES`, `TIPO_JUSTIFICACIONES`) pero **el controller/endpoint aún no está implementado en la API**.

### 4.2 Modelo de datos

```
JUSTIFICACIONES
  ├─ Id
  ├─ TrabajadorId         → trabajador que justifica
  ├─ TipoJustificacionId  → FK a TIPO_JUSTIFICACIONES
  ├─ FechaJustificada     → fecha/hora del hecho a justificar
  ├─ Motivo               → descripción libre
  ├─ DocumentoAdjuntoUrl  → URL del archivo adjunto (si aplica)
  ├─ IdEstado             → estado vía MAESTRO_ESTADOS
  ├─ FechaAutorizacion    → cuando fue aprobada/rechazada
  ├─ UsuarioAutoriza      → nombre del autorizador
  └─ IdAutoriza           → id_trabajador del autorizador

TIPO_JUSTIFICACIONES
  ├─ Id
  ├─ NombreTipo           → ej: "Permiso médico", "Tardanza justificada"
  ├─ RequiereAdjunto      → si el tipo exige subir documento
  └─ EsActivo
```

### 4.3 Flujo esperado (pendiente de implementar)

```
Trabajador crea justificación
    POST /api/Justificaciones
        └─ TrabajadorId, TipoJustificacionId, FechaJustificada, Motivo, Adjunto?
        └─ Estado inicial → PENDIENTE (IdEstado)

Jefe revisa
    PUT /api/Justificaciones/{id}/aprobar   → estado APROBADO
    PUT /api/Justificaciones/{id}/rechazar  → estado RECHAZADO

El cierre diario (SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA) debería considerar
las justificaciones APROBADAS para modificar el EstadoAsistencia en
ASISTENCIA_RESUMEN_DIARIO (ej: FALTA → FALTA_JUSTIFICADA).
```

---

## 5. Relación entre los tres módulos

```
MARCACIONES_ASISTENCIA
      │
      ▼ (cada 3 min / manual)
SP_PROCESAR_CIERRE_DIARIO
      │
      ▼
ASISTENCIA_RESUMEN_DIARIO
      │
      ├── IdCoberturaOrigen → COBERTURA_TURNOS (si el día fue cubierto)
      └── EstadoAsistencia puede modificarse por JUSTIFICACIONES aprobadas


COBERTURA_TURNOS
  └─ Afecta qué trabajador debe estar presente en qué turno.
     El SP de cierre debería verificar coberturas aprobadas para
     asignar correctamente la asistencia.

JUSTIFICACIONES
  └─ Pendiente de implementar el controller.
     Una vez activo, el SP de cierre debe consultarlas para
     convertir FALTA → FALTA_JUSTIFICADA en el resumen diario.
```

---

## 6. Configuración de horarios (referencia rápida)

```
TIPO_TURNO          → "FIJO" | "ROTATIVO"
TURNOS              → agrupa HorariosTurno
HORARIOS_TURNO      → un conjunto de detalles (NombreHorario, EsActivo)
HORARIOS_DETALLE    → un registro por día/grupo de días:
    ├─ DiaSemana          → "1-5", "Lun-Vie", "1,2,3", "Lunes", etc.
    ├─ HoraInicio
    ├─ HoraFin
    ├─ SalidaDiaSiguiente → true si el turno cruza medianoche
    └─ TiempoRefrigerioMinutos

ASIGNACIONES_TURNO  → vincula un trabajador a un turno (con vigencia)
PROGRAMACION_TURNOS_SEMANAL (PTS)
    → programación día a día para rotativos
    → puede marcar EsDescanso, EsDiaBoleta, EsVacaciones
```

---

## 7. Notas técnicas relevantes

- **Tolerancia de ventana:** `EarlyWindowTolerance = LateWindowTolerance = 2 horas`
- **Anti-duplicado:** marcaciones del mismo tipo dentro de 120 segundos son rechazadas
- **Doble turno:** las SALIDA de un turno anterior no bloquean la ENTRADA del siguiente; se filtran por `FechaHora < ScheduledStart` del turno actual
- **Overnight exacto a 00:00:** el match se hace pasando la fecha base exacta (`matchedBaseDate`) para evitar calcular mal el `ScheduledStart` de turnos que terminan en medianoche exacta
- **Sede física vs. sede de pertenencia:** `SucursalId` = sede del trabajador (reportes); `SucursalMarcacionId` = sede donde marcó (auditoría de comisiones)
- **Jefe inmediato automático:** al crear un trabajador vía `POST /api/Rrhh/Trabajadores/crear-completo`, si no se envía `jefeInmediatoId`, el backend lo toma del claim `trabajador_id` del JWT del usuario logueado
