# ❓ Preguntas Frecuentes - API Descansos

## ¿Cuál es el endpoint exacto?

**Para cargar semana:**
```
POST https://127.0.0.1:7209/api/Descansos/semana
```

**Para obtener semana:**
```
GET https://127.0.0.1:7209/api/Descansos/{idTrabajador}/{semana}
```

---

## ¿Qué es `FechaLunes`?

Es la **fecha de referencia de la semana**. El sistema automáticamente:

1. Recibe cualquier fecha (puede ser lunes, martes, viernes, etc.)
2. Calcula el lunes de esa semana
3. Usa ese lunes para grabar toda la semana

**Ejemplos:**
- Envías `2026-03-20` (Viernes) → Sistema usa `2026-03-16` (Lunes)
- Envías `2026-03-16` (Lunes) → Sistema usa `2026-03-16` (Lunes)
- Envías `2026-03-22` (Domingo) → Sistema usa `2026-03-16` (Lunes)

---

## ¿Qué significa `diaDescanso`?

Es el **día semanal de descanso obligatorio** del trabajador.

**Valores (0-6):**
```
0 = Lunes es el descanso semanal
1 = Martes es el descanso semanal
2 = Miércoles es el descanso semanal
3 = Jueves es el descanso semanal
4 = Viernes es el descanso semanal
5 = Sábado es el descanso semanal
6 = Domingo es el descanso semanal
```

**En BD:**
- Se marca con `es_descanso = 1` en la tabla `PROGRAMACION_DESCANSOS`

---

## ¿Qué significa `diasBoleta`?

Son los **días de boleta/vacaciones** dentro de la semana (además del descanso regular).

**Ejemplo:**
```json
{
  "idTrabajador": 1,
  "diaDescanso": 0,
  "diasBoleta": [0, 6]
}
```

Significa:
- Lunes (0) = Descanso regular + Boleta
- Domingo (6) = Boleta (aunque no sea su descanso regular)

En BD se marca con `es_dia_boleta = 1`.

---

## ¿Puedo dejar `diasBoleta` vacío?

**Sí, completamente**. Significa que el trabajador solo tiene su descanso regular.

```json
{
  "idTrabajador": 2,
  "diaDescanso": 1,
  "diasBoleta": []
}
```

---

## ¿Qué pasa si cargo la misma semana dos veces?

El Stored Procedure **actualiza** los registros (UPSERT):

1. **Primera carga:** Inserta 7 registros
2. **Segunda carga:** Actualiza esos mismos 7 registros

No hay duplicados, se actualiza la información.

---

## ¿Cuántos registros se generan?

**Siempre 7** (uno por cada día de la semana), por cada trabajador.

**Fórmula:**
```
Total registros = Número de trabajadores × 7 días
```

**Ejemplos:**
- 1 trabajador = 7 registros
- 5 trabajadores = 35 registros
- 100 trabajadores = 700 registros

---

## ¿Debo enviar todos los días de la semana?

**No**. Especificas solo:
- El día de descanso regular (`diaDescanso`)
- Los días adicionales de boleta (`diasBoleta`)

El sistema automáticamente **calcula e inserta los 7 días**.

---

## ¿Qué sucede con los otros 6 días (si descanso semanal es lunes)?

Si `diaDescanso = 0` (Lunes):

```
Lunes    (0) → es_descanso = 1, es_dia_boleta = (según diasBoleta)
Martes   (1) → es_descanso = 0, es_dia_boleta = 0
Miércoles(2) → es_descanso = 0, es_dia_boleta = 0
Jueves   (3) → es_descanso = 0, es_dia_boleta = 0
Viernes  (4) → es_descanso = 0, es_dia_boleta = 0
Sábado   (5) → es_descanso = 0, es_dia_boleta = 0
Domingo  (6) → es_descanso = 0, es_dia_boleta = 0
```

Son **días laborales normales** (excepto si están en `diasBoleta`).

---

## ¿Se puede asignar un día laboral como boleta?

**Sí**. Puedes poner cualquier día en `diasBoleta`, incluso si no es descanso semanal.

**Ejemplo:**
```json
{
  "idTrabajador": 1,
  "diaDescanso": 0,
  "diasBoleta": [3, 4, 5]
}
```

Significa:
- Lunes (0) = Descanso regular
- Jueves (3), Viernes (4), Sábado (5) = Boleta/Vacaciones adicionales

---

## ¿Puedo poner el mismo día en descanso y boleta?

**Sí, automáticamente**. Si `diaDescanso = 0` y incluyes `0` en `diasBoleta`:

```json
{
  "idTrabajador": 1,
  "diaDescanso": 0,
  "diasBoleta": [0, 6]
}
```

En BD ese día tendrá:
- `es_descanso = 1`
- `es_dia_boleta = 1`

Ambos flags activados (es correcto y no hay conflicto).

---

## ¿Necesito incluir el 0 en `diasBoleta` si es el descanso?

**No es obligatorio**, pero no hay problema si lo incluyes. El sistema lo maneja bien:

```json
// Opción A: Sin incluir
{
  "diaDescanso": 0,
  "diasBoleta": [6]
}
// Resultado: Lunes descanso, Domingo boleta

// Opción B: Incluyendo
{
  "diaDescanso": 0,
  "diasBoleta": [0, 6]
}
// Resultado: Lunes descanso Y boleta, Domingo boleta
```

Ambas funcionan, es cuestión de preferencia.

---

## ¿Cómo consulto qué descansos tiene un trabajador?

Usa el GET:

```bash
GET /api/Descansos/{idTrabajador}/{cualquierFechaDeLaSemana}
```

El sistema automáticamente:
1. Identifica el lunes de esa semana
2. Retorna los 7 días
3. Indica `esDescanso` y `esDiaBoleta` para cada día

---

## ¿Qué pasa si consulto una fecha sin semana cargada?

El GET retorna la semana con todos los días en estado:
```json
{
  "fecha": "2026-03-16",
  "esDescanso": false,
  "esDiaBoleta": false
}
```

Significa que **no hay datos** para esa semana (aún no se cargó).

---

## ¿La API valida que el trabajador existe?

**No en el controlador**. Las validaciones son:

✅ Al menos 1 trabajador
✅ DiaDescanso ∈ [0-6]
✅ DiasBoleta[i] ∈ [0-6]

❌ **No valida** que `idTrabajador` exista en `TRABAJADORES`

El Stored Procedure probablemente manejará esto (dependerá de las FK en BD).

---

## ¿Necesito autenticación?

**Sí, siempre**. Requiere Bearer token en el header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## ¿Qué roles pueden cargar semanas?

Solo estos:
```
ADMIN
SUPERADMIN
SUPERVISOR
```

Los demás roles (TRABAJADOR, etc.) reciben **403 Forbidden**.

---

## ¿Puedo actualizar solo un día?

**No directamente en este endpoint**. Debes cargar la **semana completa** con los 7 días.

Si necesitas cambiar solo 1 día, debes:
1. Obtener la semana actual
2. Modificar el día que necesites
3. Cargar la semana completa nuevamente

---

## ¿Existe un endpoint para eliminar?

**No en los controladores actuales**. Para limpiar una semana:

1. Cargar con `diaDescanso` y `diasBoleta` en valores neutros
2. O acceder directamente a BD con SQL

---

## ¿Cómo se calcula el lunes?

El código usa esta lógica:

```csharp
private static DateTime GetMonday(DateTime date)
{
    var diff = ((int)date.DayOfWeek + 6) % 7;
    return date.AddDays(-diff);
}
```

**Funciona así:**
- Toma el `DayOfWeek` (0=Domingo, 1=Lunes, ..., 6=Sábado)
- Calcula cuántos días atrás es el lunes
- Resta esos días a la fecha

**Ejemplos:**
- Viernes (5) → diff = 4 → resta 4 días → Lunes
- Domingo (0) → diff = 6 → resta 6 días → Lunes anterior
- Lunes (1) → diff = 0 → no resta nada → Lunes

---

## ¿Qué significa el XML que se envía internamente?

```xml
<semana>
  <t id="1" desc="0" bol="0,6" />
  <t id="2" desc="1" bol="" />
  <t id="3" desc="2" bol="3" />
</semana>
```

**Desglose:**
```
<t>      = elemento para trabajador (t = trabajador)
id       = idTrabajador
desc     = diaDescanso
bol      = diasBoleta separados por comas
```

El SP parsea este XML y ejecuta lógica en SQL.

---

## ¿Puedo cargar múltiples semanas en un request?

**No, solo una semana por request**. 

Pero con **múltiples trabajadores en esa misma semana**.

Para cargar varias semanas, haz múltiples POSTs.

---

## ¿Cómo se vería en una app?

**Flujo típico:**

```
1. Admin abre calendarioUI
2. Selecciona semana (ej: 16-22 marzo)
3. Para cada trabajador:
   - Asigna día de descanso
   - Asigna días de boleta
4. Click en "Guardar semana"
   → POST /api/Descansos/semana
5. Response exitosa
6. Sistema recarga calendarios
   → GET /api/Descansos/{idTrab}/2026-03-20
7. Se muestran los descansos en UI
```

---

## ¿Hay limpieza de datos antiguos?

**No indicada en el código**. Los datos históricos se mantienen.

Para limpiar semanas antiguas, necesitarías:
- Una política de limpieza
- Un endpoint DELETE (que no existe)
- O SQL manual en BD

---

## Resumen Rápido

| Pregunta | Respuesta |
|----------|-----------|
| **¿Qué es?** | Sistema de asignación semanal de descansos |
| **Endpoints** | POST /semana, GET /{id}/{fecha} |
| **Registros** | 7 por trabajador, por semana |
| **Validación** | Días 0-6, al menos 1 trabajador |
| **Roles** | ADMIN, SUPERADMIN, SUPERVISOR |
| **Auto-calc** | Sí, calcula lunes automáticamente |
| **Actualizable** | Sí, carga la misma semana dos veces |
| **Eliminar** | No hay endpoint, usa SQL o recarga |
