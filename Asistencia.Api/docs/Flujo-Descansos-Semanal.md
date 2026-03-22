# 🔄 Flujo de Asignación de Semana de Descansos

## Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│                    CLIENTE (Web/Mobile)                         │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              API REST (ASP.NET Core 8)                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  POST /api/Descansos/semana                              │  │
│  │  GET  /api/Descansos/{idTrabajador}/{semana}             │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│          DescansosController.cs                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ CargarSemana()  → Valida → Genera XML → Ejecuta SP      │  │
│  │ GetSemana()     → Consulta → Mapea    → Retorna JSON    │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│      SQL Server (Base de Datos)                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ EXEC SP_CARGAR_SEMANA_DESCANSOS @FechaLunes, @DatosXML  │  │
│  │                                    ↓                      │  │
│  │  UPDATE/INSERT PROGRAMACION_DESCANSOS                    │  │
│  │  (7 registros por trabajador, uno por día)               │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Proceso de Cargar Semana (POST)

```
PASO 1: Request llega con JSON
┌─────────────────────────────────────┐
│ {                                   │
│   "fechaLunes": "2026-03-16",       │
│   "trabajadores": [                 │
│     {                               │
│       "idTrabajador": 1,            │
│       "diaDescanso": 0,             │
│       "diasBoleta": [0, 6]          │
│     }                               │
│   ]                                 │
│ }                                   │
└─────────────────────────────────────┘
              ↓
PASO 2: Validaciones
┌─────────────────────────────────────┐
│ ✓ trabajadores.Count > 0            │
│ ✓ diaDescanso ∈ [0-6]               │
│ ✓ diasBoleta[i] ∈ [0-6] ∀i          │
└─────────────────────────────────────┘
              ↓
PASO 3: Conversión a XML
┌─────────────────────────────────────┐
│ <semana>                            │
│   <t id="1" desc="0" bol="0,6" />   │
│ </semana>                           │
└─────────────────────────────────────┘
              ↓
PASO 4: Ejecución de SP
┌─────────────────────────────────────┐
│ EXEC SP_CARGAR_SEMANA_DESCANSOS     │
│   @FechaLunes = '2026-03-16'        │
│   @DatosXML = '<semana>...'         │
└─────────────────────────────────────┘
              ↓
PASO 5: Response exitosa
┌─────────────────────────────────────┐
│ {                                   │
│   "ok": true,                       │
│   "mensaje": "Semana cargada.",     │
│   "fechaLunes": "2026-03-16",       │
│   "fechaFin": "2026-03-22"          │
│ }                                   │
└─────────────────────────────────────┘
```

---

## Qué se graba en Base de Datos

```
ENTRADA: Trabajador 1, Semana del 16-03-2026
  diaDescanso = 0 (Lunes)
  diasBoleta = [0, 6] (Lunes y Domingo)

        ↓

SALIDA: 7 registros insertados en PROGRAMACION_DESCANSOS

┌─────────────┬──────────────┬──────────────┬──────────────┐
│ id_trabajador│   fecha      │es_descanso│es_dia_boleta│
├─────────────┼──────────────┼──────────────┼──────────────┤
│      1      │ 2026-03-16   │    1        │     1        │  Lunes
│      1      │ 2026-03-17   │    0        │     0        │  Martes
│      1      │ 2026-03-18   │    0        │     0        │  Miércoles
│      1      │ 2026-03-19   │    0        │     0        │  Jueves
│      1      │ 2026-03-20   │    0        │     0        │  Viernes
│      1      │ 2026-03-21   │    0        │     0        │  Sábado
│      1      │ 2026-03-22   │    0        │     1        │  Domingo
└─────────────┴──────────────┴──────────────┴──────────────┘

Total: 7 registros para 1 trabajador en 1 semana
Si cargas 10 trabajadores: 70 registros (7 × 10)
```

---

## Proceso de Obtener Semana (GET)

```
PASO 1: Request
┌──────────────────────────────────┐
│ GET /api/Descansos/1/2026-03-20  │
│ Authorization: Bearer <token>    │
└──────────────────────────────────┘
              ↓
PASO 2: Cálculo de lunes
┌──────────────────────────────────┐
│ Entrada: 2026-03-20 (Viernes)    │
│ Cálculo: GetMonday(date)         │
│ Resultado: 2026-03-16 (Lunes)    │
└──────────────────────────────────┘
              ↓
PASO 3: Consulta a BD
┌──────────────────────────────────┐
│ SELECT * FROM PROGRAMACION_      │
│   DESCANSOS                      │
│ WHERE id_trabajador = 1          │
│   AND fecha BETWEEN              │
│   2026-03-16 AND 2026-03-22      │
│ ORDER BY fecha ASC               │
└──────────────────────────────────┘
              ↓
PASO 4: Mapeo de datos
┌──────────────────────────────────┐
│ Cada registro de BD →            │
│ DescansoDiaDto {                 │
│   Fecha: string,                 │
│   EsDescanso: bool,              │
│   EsDiaBoleta: bool              │
│ }                                │
└──────────────────────────────────┘
              ↓
PASO 5: Response JSON
┌──────────────────────────────────────┐
│ {                                    │
│   "idTrabajador": 1,                 │
│   "fechaLunes": "2026-03-16",        │
│   "dias": [                          │
│     {                                │
│       "fecha": "2026-03-16",         │
│       "esDescanso": true,            │
│       "esDiaBoleta": true            │
│     },                               │
│     ...7 días totales...             │
│   ]                                  │
│ }                                    │
└──────────────────────────────────────┘
```

---

## Flujo Completo en Timeline

```
TIEMPO:  Cliente          →  API            →  BD
────────────────────────────────────────────────────

T1:      POST /semana
         ├─ 4 trabajadores
         └─ 7 días cada uno
                          →  Validaciones ✓
                          →  Genera XML
                          →  EXEC SP
                                        → INSERT
                                        → 28 registros
                                        ← OK

T2:      ← Response 200
         {
           ok: true,
           mensaje: "Semana cargada"
         }

T3:      GET /1/2026-03-20
                          → Query BD
                          → 7 registros
                                        ← 7 rows

T4:      ← Response 200
         {
           idTrabajador: 1,
           dias: [...]
         }

T5:      GET /2/2026-03-20
                          → Query BD
                          → 7 registros
                                        ← 7 rows

T6:      ← Response 200
         {
           idTrabajador: 2,
           dias: [...]
         }
```

---

## Mapeo de Días (0-6)

```
DIA (int)  NOMBRE (ES)      NOMBRE (EN)     EJEMPLO
─────────────────────────────────────────────────────
   0      Lunes            Monday          2026-03-16
   1      Martes           Tuesday         2026-03-17
   2      Miércoles        Wednesday       2026-03-18
   3      Jueves           Thursday        2026-03-19
   4      Viernes          Friday          2026-03-20
   5      Sábado           Saturday        2026-03-21
   6      Domingo          Sunday          2026-03-22
```

---

## Estado de Base de Datos (Antes vs Después)

```
ANTES de cargar:
┌─────────────────────────────────────┐
│  PROGRAMACION_DESCANSOS             │
├─────────────────────────────────────┤
│  (vacía o contiene semanas antiguas)│
└─────────────────────────────────────┘

DESPUÉS de: POST /api/Descansos/semana
           con 3 trabajadores:
┌─────────────────────────────────────┐
│  PROGRAMACION_DESCANSOS             │
├────┬──────────┬────────┬──────────┤
│ id │ trab_id  │ fecha  │ descanso │
├────┼──────────┼────────┼──────────┤
│ 1  │    1     │ 16/03  │    1     │  Trab 1
│ 2  │    1     │ 17/03  │    0     │
│ 3  │    1     │ 18/03  │    0     │
│ 4  │    1     │ 19/03  │    0     │
│ 5  │    1     │ 20/03  │    0     │
│ 6  │    1     │ 21/03  │    0     │
│ 7  │    1     │ 22/03  │    0     │
│ 8  │    2     │ 16/03  │    0     │  Trab 2
│ 9  │    2     │ 17/03  │    1     │
│10  │    2     │ 18/03  │    0     │
│11  │    2     │ 19/03  │    0     │
│12  │    2     │ 20/03  │    0     │
│13  │    2     │ 21/03  │    0     │
│14  │    2     │ 22/03  │    0     │
│15  │    3     │ 16/03  │    0     │  Trab 3
│16  │    3     │ 17/03  │    0     │
│17  │    3     │ 18/03  │    1     │
│18  │    3     │ 19/03  │    0     │
│19  │    3     │ 20/03  │    0     │
│20  │    3     │ 21/03  │    0     │
│21  │    3     │ 22/03  │    0     │
└────┴──────────┴────────┴──────────┘

TOTAL: 21 registros insertados (3 trabajadores × 7 días)
```

---

## Notas sobre el Stored Procedure

El SP `SP_CARGAR_SEMANA_DESCANSOS` recibe:

```sql
@FechaLunes DATETIME   -- Primer día de la semana
@DatosXML XML          -- Estructura de trabajadores y descansos
```

Y ejecuta internamente algo como:

```sql
-- Parsear XML
-- Para cada trabajador:
--   Para cada día de la semana:
--     INSERT o UPDATE en PROGRAMACION_DESCANSOS
--     asignando es_descanso y es_dia_boleta
```

Esto es transparente para el cliente - solo envías el JSON y el sistema se encarga del resto.
