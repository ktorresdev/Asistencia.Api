Guía para frontend: consumir los nuevos endpoints de Turnos / Programación

Resumen
- Objetivo: permitir que el frontend deje de inferir por nombre y use datos firmes del backend: `horarioTurnoId`, catálogo de motivos, tipoTurno.codigo y endpoints de programación semanal/publicar/copiar.
- Archivo en repo: `docs/frontend-consumo-turnos.md`

Autenticación y permisos
- Todos los endpoints requieren autenticación Bearer JWT.
- Endpoints sensibles requieren permisos/roles (ej. asignar, publicar, copiar). El backend expone roles en los controladores; idealmente el frontend consulte `/api/Auth/Capacidades` (si se implementa) y habilite/oculte botones.

Encabezados HTTP
- Authorization: `Bearer <token>`
- Content-Type: `application/json`

1) Asignar turno (con horario)
- URL: `POST /api/trabajadores/{trabajadorId}/asignar-turno`
- Permisos: `ADMIN,SUPERADMIN,SUPERVISOR` (backend)
- Request body (JSON):
  {
    "turnoId": 5,
    "horarioTurnoId": 12,          // opcional pero recomendado para rotativos
    "fechaInicioVigencia": "2026-03-16",
    "fechaFinVigencia": "2026-06-16",
    "motivoCambio": "CAMBIO_TEMPORAL", // usar catálogo
    "aprobadoPorTrabajadorId": 2
  }
- Responses:
  - 201 Created: body con resumen: `{ asignacionId, trabajadorId, turnoId, fechaInicioVigencia, fechaFinVigencia, esVigente }`
  - 400 Bad Request: validación de fechas
  - 404 Not Found: trabajador o turno inexistente
  - 409 Conflict: solapamiento de vigencias (si el backend lo implementa)

Frontend: al crear asignación, enviar `horarioTurnoId` cuando se seleccione un horario concreto. Si no se envía, backend seguirá la estrategia por defecto (p.ej. primer horario activo del turno).

2) Obtener turno vigente completo
- URL: `GET /api/trabajadores/{trabajadorId}/turno-vigente`
- Permisos: autenticado
- Response body (200):
  {
    "trabajadorId": 45,
    "asignacionId": 123,
    "turno": { "id": 5, "codigo": "ROT-A", "tipoTurnoId": 2, "esActivo": true },
    "vigencia": { "inicio": "2026-03-16", "fin": "2026-06-16", "esVigente": true },
    "horario": {
      "idHorarioTurno": 12,
      "nombreHorario": "Semana A",
      "detalles": [
        { "diaSemana": "Lunes", "horaInicio": "08:00", "horaFin": "16:00", "salidaDiaSiguiente": false },
        ...
      ]
    }
  }
- 404: si no hay asignación vigente

Frontend: usar este endpoint para mostrar el turno/horario activo en la ficha del trabajador y en encabezados. Evitar mostrar solo `turnoId`.

3) Listar asignaciones (paginado)
- URL: `GET /api/Rrhh/AsignacionTurno?pageNumber=1&pageSize=20`
- Permisos: autenticado (o según backend)
- Response: `PagedResult<AsignacionTurnoDto>`; cada item debe exponer `id, trabajadorId, turnoId, turnoNombre, horarioTurnoId, horarioTurnoNombre, fechaInicioVigencia, fechaFinVigencia, motivoCodigo, aprobadoPor`.

Frontend: reemplazar datos que infieren horario por `horarioTurnoId` y `horarioTurnoNombre` cuando estén presentes.

4) Programación semanal por rango de fechas
- URL: `GET /api/Rrhh/ProgramacionSemanal?fechaInicio=YYYY-MM-DD&fechaFin=YYYY-MM-DD`
- Permisos: autenticado
- Response (sugerido):
  {
    "fechaInicio": "2026-03-16",
    "fechaFin": "2026-03-22",
    "items": [
      {
        "trabajadorId": 45,
        "trabajadorNombre": "Juan Perez",
        "dias": [
          { "fecha": "2026-03-16", "horarioTurnoId": 12, "horarioTurnoNombre": "Semana A", "turnoId": 5, "turnoNombre": "Rotativo A", "estado": "planificado" },
          ...
        ]
      },
      ...
    ]
  }

Frontend: consumir por rango cuando el usuario cambia semana. Renderizar por fecha real (no por `diaSemana` fijo). Esto permite rotativos que cambian de patrón entre semanas.

5) Publicar semana
- URL: `POST /api/Rrhh/ProgramacionSemanal/publicar`
- Permisos: requiere capacidad `PUBLISH_PROGRAMACION` (o role ADMIN)
- Request body:
  { "fechaInicio": "2026-03-16", "fechaFin": "2026-03-22", "publicadoPorId": 2 }
- Response: 200 OK con resumen { publicadas: n, errores: [...] } o 202 Accepted si es async

Frontend: botón "Publicar semana" enviará este request y mostrará resultado/errores.

6) Copiar semana
- URL: `POST /api/Rrhh/ProgramacionSemanal/copiar`
- Permisos: requiere capacidad `COPY_PROGRAMACION`
- Request body:
  { "semanaOrigenInicio": "2026-03-09", "semanaDestinoInicio": "2026-03-16", "usuarioId": 2, "overwrite": false }
- Response: 200 OK con resumen de cambios

Frontend: botón copia debe indicar semana origen/destino y `overwrite` opcional.

7) Catálogos útiles
- `GET /api/Catalogos/MotivosCambio` -> [{ "codigo": "CAMBIO_TEMPORAL", "nombre": "Cambio temporal" }, ...]
- `GET /api/Catalogos/TipoTurno` -> [{ "id": 1, "codigo": "ROT", "nombre": "Rotativo" }, { "id": 2, "codigo": "FIJ", "nombre": "Fijo" }]

Frontend: usar `motivoCodigo` del catálogo en selects; usar `tipoTurno.codigo` (no el nombre) para lógica ROT/FIJ.

Errores y manejo
- 400 Bad Request: validar y mostrar errores de formulario (fechas, campos requeridos).
- 401 Unauthorized: token inválido/ausente -> redirigir a login.
- 403 Forbidden: permisos insuficientes -> ocultar botones; si ocurre, mostrar mensaje.
- 404 Not Found: recurso no existe.
- 409 Conflict: (posible) solapamiento de vigencias.

Recomendaciones de UI/UX
- No inferir por nombre: usar `tipoTurno.codigo` y `horarioTurnoId`.
- Semana en la UI: siempre pedir programación por rango de fechas y renderizar cada fecha con su `horarioTurnoId`.
- Al asignar un turno rotativo, forzar la selección de `horarioTurnoId` si la política lo exige.
- Habilitar/deshabilitar botones según `/api/Auth/Capacidades` (si está disponible); si no, basarse en roles mínimos.

Compatibilidad con código frontend actual
- Campos nuevos a mapear en modelos TS:
  - `AsignacionTurnoDto`: agregar `horarioTurnoId?: number`, `horarioTurnoNombre?: string`, `motivoCodigo?: string`, `aprobadoPor?: number`.
  - `AsignacionTurnoCreate/Update` (requests): agregar `horarioTurnoId?: number`, `motivoCodigo?: string`, `aprobadoPorTrabajadorId?: number`.

Ejemplo mínimo de fetch (asignar)
- fetch(`/api/trabajadores/${id}/asignar-turno`, { method: 'POST', headers: { 'Authorization': 'Bearer '+token, 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })

Notas finales
- El backend ya añadió `horarioTurnoId` en la entidad `AsignacionTurno` y el endpoint `GET /api/trabajadores/{id}/turno-vigente` prioriza el `HorarioTurno` asignado.
- Si quieres, puedo generar DTOs de respuesta en backend (`AsignacionTurnoDto`, `ProgramacionSemanalResponse`) con contratos exactos y actualizar los controladores para devolver esos DTOs en lugar de `object`/anónimos.

¿Deseas que también genere los DTOs de salida y actualice los controladores para devolver esos DTOs formales (mejor para frontend)?
