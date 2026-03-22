# 🎨 DIAGRAMA: Visual de Joins y Lógica

## 📊 Estructura de Tablas y Relaciones

```
TRABAJADORES (50 registros)
    ├─ id_trabajador ← PRIMARY KEY
    ├─ id_persona → PERSONAS
    ├─ id_sucursal → SUCURSAL
    ├─ correo_trabajo
    └─ fecha_ingreso
    
        PERSONAS (50 registros)
        ├─ id_persona ← PRIMARY KEY
        ├─ apellidos_nombres
        └─ dni
        
        SUCURSAL (5 registros)
        ├─ id_sucursal ← PRIMARY KEY
        ├─ nombre
        └─ ubicacion
    
    ASIGNACION_TURNO (47 registros)  ← CLAVE
    ├─ id ← PRIMARY KEY
    ├─ id_trabajador → TRABAJADORES
    ├─ id_turno → TURNOS
    ├─ id_horario_turno → HORARIOS_TURNO
    ├─ es_vigente (true/false)
    ├─ fecha_inicio_vigencia
    └─ fecha_fin_vigencia
        
        TURNOS (10 registros)
        ├─ id ← PRIMARY KEY
        ├─ nombre_codigo
        └─ es_activo
        
        HORARIOS_TURNO (30 registros)
        ├─ id ← PRIMARY KEY
        ├─ id_turno → TURNOS
        ├─ nombre_horario
        └─ es_activo
            
            HORARIOS_DETALLE (210 registros)
            ├─ id ← PRIMARY KEY
            ├─ id_horario_turno → HORARIOS_TURNO
            ├─ dia_semana (0-6)
            ├─ hora_entrada
            ├─ hora_salida
            └─ minutos_duracion
```

---

## 🔗 Visualización del LEFT JOIN

```
TRABAJADORES (50 registros)
├─ ID 1: Juan Pérez       ──LEFT JOIN──→ ASIGNACION_TURNO ✅ (Existe)
├─ ID 2: María García     ──LEFT JOIN──→ ASIGNACION_TURNO ❌ (NULL)
├─ ID 3: Carlos López     ──LEFT JOIN──→ ASIGNACION_TURNO ✅ (Existe)
├─ ID 4: Ana Rodríguez    ──LEFT JOIN──→ ASIGNACION_TURNO ❌ (NULL)
├─ ID 5: Roberto Martínez ──LEFT JOIN──→ ASIGNACION_TURNO ✅ (Existe)
...
└─ ID 50: Última persona  ──LEFT JOIN──→ ASIGNACION_TURNO ❌ (NULL)
```

**Resultado del LEFT JOIN:**
```
Conserva TODOS los 50 trabajadores
- 47 con valores en ASIGNACION_TURNO
- 3 con NULL en ASIGNACION_TURNO ← ESTOS NO TIENEN TURNO
```

---

## 📈 Lógica WHERE para Identificar SIN Turno

```
RESULTADOS DEL LEFT JOIN (50 filas)
│
├─ Fila 1: Juan (AT.ID = 5)           ✅ Con turno
├─ Fila 2: María (AT.ID = NULL)       ❌ SIN turno ← AQUÍ
├─ Fila 3: Carlos (AT.ID = 12)        ✅ Con turno
├─ Fila 4: Ana (AT.ID = NULL)         ❌ SIN turno ← AQUÍ
├─ Fila 5: Roberto (AT.ID = 23)       ✅ Con turno
└─ Fila 6: Patricia (AT.ID = NULL)    ❌ SIN turno ← AQUÍ

APLICAR: WHERE at.id IS NULL
         ↓
RESULTADO FINAL: Solo 3 filas (SIN turno)
```

---

## 🎯 Flujo de Datos en la Query

```
1. SELECT trabajador, persona, sucursal
   ↓
2. FROM TRABAJADORES t
   │
   ├─ INNER JOIN PERSONAS p (obtener apellido, dni)
   │
   ├─ LEFT JOIN SUCURSAL s (obtener nombre sucursal)
   │
   ├─ LEFT JOIN ASIGNACION_TURNO at ← CRUCIAL
   │  ├─ IF at EXISTS → Trabaja con AT.ID
   │  └─ IF at NOT EXISTS → at.id = NULL
   │
   ├─ LEFT JOIN TURNOS trn (obtener nombre turno)
   │  ├─ IF at.id IS NOT NULL → Obtiene turno
   │  └─ IF at.id IS NULL → trn.nombre_codigo = NULL
   │
   └─ LEFT JOIN HORARIOS_TURNO ht (obtener horario)
      ├─ IF at.id IS NOT NULL → Obtiene horario
      └─ IF at.id IS NULL → ht.nombre_horario = NULL

3. WHERE at.id IS NULL  ← Filtra a los SIN ASIGNACIÓN
   ↓
4. ORDER BY apellidos_nombres
   ↓
5. RESULTADO FINAL: Solo 3 trabajadores sin turno
```

---

## 📊 Comparación: INNER vs LEFT vs FULL OUTER

```
INNER JOIN ASIGNACION_TURNO
├─ Resultado: 47 registros
├─ Contiene: SOLO trabajadores con turno
└─ Falta: 3 sin turno (❌ PERDIDOS)

LEFT JOIN ASIGNACION_TURNO
├─ Resultado: 50 registros
├─ Contiene: 47 con turno + 3 sin turno (con NULL)
└─ WHERE at.id IS NULL → 3 registros sin turno ✅

FULL OUTER JOIN (no se usa en este caso)
├─ Resultado: 50 registros
└─ Útil cuando hay asignaciones huérfanas
```

---

## 🔍 Validación Paso a Paso

```
Query ejecutándose:

PASO 1: TRABAJADORES (50 filas)
┌─────────────────┐
│ ID │ NOMBRE    │
├─────┼──────────┤
│ 1  │ Juan     │
│ 2  │ María    │
│ 3  │ Carlos   │
│ 4  │ Ana      │
│ 5  │ Roberto  │
└─────────────────┘

PASO 2: LEFT JOIN ASIGNACION_TURNO (47 matches)
┌─────────────────┬────────┐
│ ID │ NOMBRE    │ AT.ID  │
├─────┼───────────┼────────┤
│ 1  │ Juan      │   5    │ ✅
│ 2  │ María     │ NULL   │ ❌ SIN TURNO
│ 3  │ Carlos    │  12    │ ✅
│ 4  │ Ana       │ NULL   │ ❌ SIN TURNO
│ 5  │ Roberto   │  23    │ ✅
│ ..              │ ...    │
│ 50 │ Patricia  │ NULL   │ ❌ SIN TURNO
└─────────────────┴────────┘

PASO 3: WHERE at.id IS NULL (filtra)
┌─────────────────┬────────┐
│ ID │ NOMBRE    │ AT.ID  │
├─────┼───────────┼────────┤
│ 2  │ María     │ NULL   │ ← RESULTADO
│ 4  │ Ana       │ NULL   │ ← RESULTADO
│ 50 │ Patricia  │ NULL   │ ← RESULTADO
└─────────────────┴────────┘

RESULTADO FINAL: 3 trabajadores sin turno
```

---

## 📈 Diagrama de Flujo Completo

```
INICIO
  │
  ├─ 1. Obtener todos los trabajadores (50)
  │     SELECT FROM TRABAJADORES
  │     │
  │     ├─ Incluir: ID, Nombre, DNI
  │     └─ Orden: Alfabético
  │
  ├─ 2. Unir con PERSONAS (para apellidos)
  │     INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
  │     │
  │     └─ Resultado: 50 con apellidos
  │
  ├─ 3. Unir con SUCURSAL (para ubicación)
  │     LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
  │     │
  │     └─ Resultado: 50 con sucursal (o NULL)
  │
  ├─ 4. ★ Unir con ASIGNACION_TURNO ★ (LA CRUCIAL)
  │     LEFT JOIN ASIGNACION_TURNO at ON ...
  │     │
  │     ├─ 47 tienen MATCH (at.id = número)
  │     └─ 3 SIN MATCH (at.id = NULL)
  │
  ├─ 5. Unir con TURNOS (detalles)
  │     LEFT JOIN TURNOS trn ON at.id_turno = trn.id
  │     │
  │     ├─ 47 obtienen nombre turno
  │     └─ 3 quedan NULL
  │
  ├─ 6. Unir con HORARIOS_TURNO (detalles)
  │     LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
  │     │
  │     ├─ 47 obtienen horario
  │     └─ 3 quedan NULL
  │
  ├─ 7. FILTRAR por SIN ASIGNACIÓN
  │     WHERE at.id IS NULL
  │     │
  │     └─ Resultado: 3 filas
  │
  └─ FIN: Mostrar 3 trabajadores sin turno
```

---

## 🎯 La Clave: Entender NULL

```
ANTES (SIN WHERE):
┌─────────────────┬────────┬───────────────┐
│ NOMBRE    │ AT.ID  │ TURNO         │
├──────────┼────────┼───────────────┤
│ Juan     │   5    │ TURNO_MAÑANA  │
│ María    │ NULL   │ NULL          │ ← NULL = SIN TURNO
│ Carlos   │  12    │ TURNO_TARDE   │
│ Ana      │ NULL   │ NULL          │ ← NULL = SIN TURNO
└──────────┴────────┴───────────────┘

DESPUÉS (CON WHERE at.id IS NULL):
┌─────────────────┬────────┬───────────────┐
│ NOMBRE    │ AT.ID  │ TURNO         │
├──────────┼────────┼───────────────┤
│ María    │ NULL   │ NULL          │ ← MOSTRADOS
│ Ana      │ NULL   │ NULL          │ ← MOSTRADOS
└──────────┴────────┴───────────────┘
```

---

## 📊 Tabla de Estados

```
Trabajador │ AT.ID │ ES_VIGENTE │ ESTADO            │ MOSTRAR EN QUERY
───────────┼───────┼────────────┼──────────────────┼──────────────────
Juan       │  5    │ 1          │ ✅ ASIGNADO      │ No
María      │ NULL  │ NULL       │ ❌ SIN ASIGNAR   │ Sí ← Query 2
Carlos     │  12   │ 1          │ ✅ ASIGNADO      │ No
Ana        │ NULL  │ NULL       │ ❌ SIN ASIGNAR   │ Sí ← Query 2
Roberto    │  23   │ 0          │ ⚠️ VENCIDA       │ No (pero vigencia 0)
Patricia   │ NULL  │ NULL       │ ❌ SIN ASIGNAR   │ Sí ← Query 2
```

---

## 🚀 RESUMEN VISUAL

```
50 TRABAJADORES
│
├─ 47 tienen ASIGNACION_TURNO (at.id ≠ NULL) ✅
│  └─ MOSTRAR en reporte de "con turno"
│
└─ 3 NO tienen ASIGNACION_TURNO (at.id = NULL) ❌
   └─ MOSTRAR en reporte de "sin turno"
      └─ ESTOS SON LOS QUE NECESITA ASIGNAR
```

---

**La lógica es simple: LEFT JOIN mantiene todos, WHERE at.id IS NULL filtra solo los sin turno.** ✅
