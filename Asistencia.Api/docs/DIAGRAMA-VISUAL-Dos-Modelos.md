# 🎨 Diagrama Visual: Dos Modelos de Turnos

## ARQUITECTURA COMPLETA

```
                              EMPRESA
                                 │
                 ┌───────────────┼───────────────┐
                 │                               │
           ┌─────▼─────┐                  ┌─────▼─────┐
           │   FIJOS    │                  │ ROTATIVOS │
           │    70%     │                  │    30%    │
           └─────┬─────┘                  └─────┬─────┘
                 │                              │
        ┌────────┴────────┐          ┌──────────┴──────────┐
        │                 │          │                     │
   ┌────▼────┐       ┌────▼────┐   ┌▼────────────────────┐│
   │ASIGNACION│       │PROGRAMACION                       ││
   │TURNO     │       │DESCANSOS                          ││
   │(Base)    │       │(Ausencias)                        ││
   └────┬─────┘       └─────┬────┘                        │
        │                   │    PROGRAMACION_TURNOS_    ││
        │            ┌──────▼────┐  SEMANAL               ││
        │            │            │  (Flexible)           ││
        │       ┌────▼────┐       │                       ││
        │       │COBERTURA│       │  ┌─────┐             ││
        │       │TURNO    │       │  │ L   │             ││
        │       │(Cambios)│       │  │ u   │             ││
        └───┬───┴────┬────┴───┬───┘  │ n   │             ││
            │        │        │      │ e   │             ││
         ┌──▼─┐  ┌───▼───┐  ┌─▼──┐  │ s   │             ││
         │GET │  │ POST  │  │PUT │  │ -   │─────────────┘│
         │(V) │  │(A)    │  │(M) │  │ D   │  CALENDARIO  │
         │    │  │       │  │    │  │ o   │  TRABAJADOR  │
         └────┘  └───────┘  └────┘  │ m   │              │
                                    │ i   │              │
                                    │ n   │              │
                                    │ g   │              │
                                    │ o   │              │
                                    └─────┘              │
                                                         │
                                    ┌─────┐             │
                                    │ M   │             │
                                    │ a   │             │
                                    │ r   │────────────┘
                                    │ t   │
                                    │ e   │
                                    │ s   │
                                    └─────┘
```

## FLUJO DE DATOS

### TRABAJADOR FIJO

```
┌──────────────────────────────────────────────────────────┐
│ JUAN (Administrativo)                                    │
└───────────────────┬──────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        │                       │
   ┌────▼─────────┐    ┌────────▼──────────┐
   │ASIGNACION    │    │AUSENCIAS/CAMBIOS  │
   │              │    │(Excepciones)      │
   │Turno Mañana  │    │                   │
   │9am-5pm       │    │15-Mar: Ausencia   │
   │01-Ene - 31   │    │20-Mar: Cambio con │
   │Dic 2026      │    │       María       │
   └──────────────┘    └───────────────────┘
        │                       │
        │         ┌─────────────┘
        │         │
        └────┬────┴─┐
             │      │
          ┌──▼──┐┌──▼──┐
          │ 9am ││Ausencia│
          │ 5pm ││       │
          │ 1-14││15-Mar │
          └─────┘└───────┘
          
          ┌──────┐┌──────┐
          │Turno ││Turno │
          │María ││Juan  │
          │20-Mar│20-Mar│
          └──────┘└──────┘
          
          = CALENDARIO MOSTRADO
```

### TRABAJADOR ROTATIVO

```
┌──────────────────────────────────────────────────────────┐
│ MARÍA (Turnera)                                          │
└───────────────────┬──────────────────────────────────────┘
                    │
        ┌───────────▼───────────┐
        │                       │
        │ PROGRAMACION SEMANAL  │
        │                       │
   ┌────▼────┐ ┌──────┐ ┌──────┐
   │L 16-Mar │ │M     │ │X     │
   │Descanso │ │17-Mar│ │18-Mar│
   └─────────┘ │Tarde │ │Noche │
               └──────┘ └──────┘
   
   ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐
   │J 19  │ │V 20  │ │S 21  │ │D 22  │
   │Mañana│ │Mañana│ │Boleta│ │Boleta│
   └──────┘ └──────┘ └──────┘ └──────┘
   
   = CALENDARIO MOSTRADO (7 DÍAS)
   
   ¿SIGUIENTE SEMANA? → RECARGA COMPLETAMENTE DIFERENTE
```

## ENDPOINTS Y OPERACIONES

### SISTEMA FIJO

```
┌─────────────────────────────────────────┐
│ ASIGNACION_TURNO (Base a largo plazo)   │
├─────────────────────────────────────────┤
│ POST   /api/Rrhh/AsignacionTurno        │
│        Crear turno base                 │
├─────────────────────────────────────────┤
│ GET    /api/Rrhh/AsignacionTurno        │
│        Ver asignaciones activas         │
├─────────────────────────────────────────┤
│ PUT    /api/Rrhh/AsignacionTurno/{id}   │
│        Cambiar turno base               │
├─────────────────────────────────────────┤
│ DELETE /api/Rrhh/AsignacionTurno/{id}   │
│        Eliminar asignación              │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ PROGRAMACION_DESCANSOS (Ausencias)      │
├─────────────────────────────────────────┤
│ POST   /api/Descansos/semana            │
│        Marcar ausencias/boletas         │
├─────────────────────────────────────────┤
│ GET    /api/Descansos/{id}/{fecha}      │
│        Ver ausencias de semana          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ COBERTURA_TURNO (Intercambios)          │
├─────────────────────────────────────────┤
│ POST   /api/Rrhh/CoberturaTurno         │
│        Cambiar turno con compañero      │
├─────────────────────────────────────────┤
│ GET    /api/Rrhh/CoberturaTurno         │
│        Ver coberturas activas           │
└─────────────────────────────────────────┘
```

### SISTEMA ROTATIVO

```
┌────────────────────────────────────────────┐
│ PROGRAMACION_TURNOS_SEMANAL (Flexible)     │
├────────────────────────────────────────────┤
│ POST /api/Rrhh/ProgramacionSemanal         │
│      Cargar programación de la semana      │
├────────────────────────────────────────────┤
│ GET  /api/Rrhh/ProgramacionSemanal         │
│      Ver programación (7 días)             │
├────────────────────────────────────────────┤
│ GET  .../horarios-disponibles              │
│      Ver horarios válidos para asignar     │
└────────────────────────────────────────────┘
```

## CICLO DE VIDA

### FIJO

```
CREAR ASIGNACION
       │
       ▼
   ┌────────────┐
   │ VIGENCIA   │  Validar durante período
   │ (6 meses)  │  (ej: 01-01 a 30-06)
   └────┬───────┘
        │
   ┌────▼─────────────┐
   │ EXCEPCIONES      │
   ├──────────────────┤
   │ • Ausencias      │
   │ • Cambios        │
   │ (Periódicamente) │
   └──────────────────┘
        │
        ▼
   FIN DE VIGENCIA → Crear nueva asignación
```

### ROTATIVO

```
CARGAR PROGRAMACION SEMANA 1
       │
       ▼
   ┌─────────────────┐
   │ 7 DÍAS VIGENTES │
   │ (16-22 Marzo)   │
   └────┬────────────┘
        │
       ▼
  CARGAR PROGRAMACION SEMANA 2
       │
       ▼
   ┌─────────────────┐
   │ 7 DÍAS VIGENTES │
   │ (23-29 Marzo)   │
   └────┬────────────┘
        │
       ▼ (Y así cada semana...)
```

## VISTA CONSOLIDADA: CALENDARIO MENSUAL

```
                        MARZO 2026
     ┌──────┬──────┬──────┬──────┬──────┬──────┬──────┐
     │  L   │  M   │  X   │  J   │  V   │  S   │  D   │
┌────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│JUAN│MAÑANA│MAÑANA│MAÑANA│MAÑANA│MAÑANA│AUSENCIA│MAÑANA│ (FIJO)
│ 1  │      │      │      │      │      │       │      │
├────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│JUAN│MAÑANA│MAÑANA│MAÑANA│MAÑANA│CAMBIO│AUSENCIA│MAÑANA│ (CAMBIO con María)
│ 2  │      │      │      │      │ CON  │       │      │
│    │      │      │      │      │MARÍA │       │      │
├────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│MARÍA│DESC  │TARDE │NOCHE │MAÑANA│MAÑANA│BOLETA│BOLETA│ (ROTATIVO)
│ 1  │ANSO  │      │      │      │      │      │      │
├────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│MARÍA│TARDE │TARDE │TARDE │DESC  │TARDE │TARDE │TARDE │ (ROTATIVO - Semana 2)
│ 2  │      │      │      │ANSO  │      │      │      │
└────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘
```

**LEYENDA:**
- JUAN (FIJO): Base + excepciones
- MARÍA (ROTATIVA): Completamente flexible cada semana
