# Backlog técnico — Marcación / Asignación de turno / Estabilidad
(Generado 2026-06-10 tras QA intenso. Estado: QA en server 10.0.2.5)

## 🔴 Bloqueantes
- **Doble turno rompe el cierre.** `PermitirDobleTurno` crea 2 ASIGNACIONES_TURNO vigentes; el SP cierre genera 2 filas por trabajador/día → viola `UQ_Resumen_Diario (id_trabajador, fecha)` → aborta TODO el INSERT base (Filas base:0) → nadie obtiene resumen ese día. NO habilitar en prod sin resolver el modelo.
- **es_mock_location**: columna que el código exige y rompía toda marcación si falta (ya corregido en QA; correr `FIX-Marcaciones-EsMockLocation.sql` en prod).

## 🟠 Bugs / inconsistencias
- **motivo_cambio**: endpoint asignar-turno recibe texto libre, pero CHECK `CK_AT_Motivo` solo permite NECESIDAD_OPERATIVA / SOLICITUD_TRABAJADOR / CAMBIO_TEMPORAL / CAMBIO_PERMANENTE / ASIGNACION_INICIAL → puede fallar la asignación. Convertir a dropdown.
- **Validaciones de marcación comentadas**: `// Todas las validaciones han sido comentadas para permitir marcar sin restricciones`. Hoy se marca fuera de ventana. Definir y reactivar reglas.
- **Console.WriteLine("[GEOFENCE]")** de debug en el flujo de marcación → quitar / logging estructurado.

## 🎯 Doble turno — implementación completa (propuesta)
- Fase 1 (BD/Cierre): único = `(id_trabajador, fecha, id_turno)` + columna id_turno/id_asignacion; SP cierre itera por asignación vigente → 1 fila por turno.
- Fase 2 (Marcación): resolver todas las ventanas activas de las asignaciones vigentes; ENTRADA/SALIDA por turno, no global del día.
- Fase 3 (Reportes/UI): agrupar por turno; UI para asignar/listar/quitar 2º turno.

## 🧪 Marcación — pendientes
- Reactivar validaciones (ventana/duplicados/foto obligatoria si tomar_foto).
- Marcación de doble turno (asociar marca al turno por ventana).
- Mensajes/códigos de error consistentes.

## 🧪 Asignación de turno — pendientes
- motivoCambio dropdown.
- UI gestión de asignaciones vigentes (ver/quitar 2º turno).
- Validar solapamiento de horarios entre turnos.

## ⚙️ Estabilidad (ver también memoria deploy-qa-a-produccion)
- Health-check de esquema al arranque (evita "se rompió todo" silencioso).
- Resiliencia de conexión BD (EF EnableRetryOnFailure) + /health + errores de login decentes.
- Secretos fuera del repo (JWT key real, passwords). Forzar cambio de contraseña inicial.
- Migraciones EF en vez de scripts manuales (deriva de esquema).
- DTOs ligeros (la marcación devuelve el Trabajador completo anidado).
- Índices: MARCACIONES_ASISTENCIA(id_trabajador,fecha_hora), ASISTENCIA_RESUMEN_DIARIO(fecha_asistencia).
- Tests automatizados de ResolveShiftContext y del SP de cierre.
