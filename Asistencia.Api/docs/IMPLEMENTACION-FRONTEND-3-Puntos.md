# 💻 Implementación Frontend: 3 Puntos Críticos

## 🎯 Puntos a Implementar

---

## 1️⃣ WIZARD - Cargar Turno Vigente al Editar

### Problema Actual
```
Usuario abre EDITAR trabajador
  ↓
Paso 3: Selector de turno está vacío
  ↓
❌ No se carga el turno actual
```

### Solución

**Paso 1: Detectar modo (Create vs Edit)**

```typescript
// En tu componente wizard
export class WizardComponent implements OnInit {
  isEditMode: boolean = false;
  trabajadorId: number;

  ngOnInit() {
    // Si viene con ID en ruta → EDITAR
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.trabajadorId = params['id'];
        this.loadTurnoVigente(); // ← AQUÍ
      }
    });
  }
}
```

**Paso 2: Función para Cargar Turno Vigente**

```typescript
loadTurnoVigente() {
  this.trabajadorService.getTurnoVigente(this.trabajadorId)
    .subscribe({
      next: (response) => {
        // response contiene:
        // {
        //   trabajadorId: 42,
        //   asignacionId: 5,
        //   turno: { id: 1, codigo: 'TURNO_MAÑANA', ... },
        //   vigencia: { inicio: '2026-01-01', fin: '2026-12-31' }
        // }
        
        this.formData = {
          ...this.formData,
          turnoId: response.turno.id,
          asignacionId: response.asignacionId,
          tipoTurnoId: response.turno.tipoTurnoId,
          fechaInicioVigencia: response.vigencia.inicio,
          fechaFinVigencia: response.vigencia.fin
        };

        // ← IMPORTANTE: Guardar asignacionId para luego hacer PUT
        this.currentAsignacionId = response.asignacionId;
      },
      error: (err) => {
        console.warn('Sin turno vigente (primera asignación)');
        // Es primera asignación → usar POST
      }
    });
}
```

**Paso 3: Servicio Angular**

```typescript
// trabajador.service.ts
getTurnoVigente(trabajadorId: number): Observable<any> {
  return this.http.get<any>(
    `${this.apiUrl}/trabajadores/${trabajadorId}/turno-vigente`
  );
}
```

**Paso 4: En el Template**

```html
<!-- Step 3: Turno -->
<div *ngIf="currentStep === 3">
  <h3>Asignar Turno</h3>
  
  <!-- Selector de Turno -->
  <select formControlName="turnoId" (change)="onTurnoChange($event)">
    <option [value]="null">-- Selecciona Turno --</option>
    <option *ngFor="let turno of turnos" [value]="turno.id">
      {{ turno.nombreCodigo }}
    </option>
  </select>

  <!-- Selector de Horario (dinámico según turno) -->
  <select formControlName="horarioTurnoId">
    <option [value]="null">-- Selecciona Horario --</option>
    <option *ngFor="let horario of horariosFiltrados" [value]="horario.id">
      {{ horario.nombreHorario }}
    </option>
  </select>

  <!-- Fechas -->
  <input type="date" formControlName="fechaInicioVigencia" required>
  <input type="date" formControlName="fechaFinVigencia">

  <button (click)="guardarAsignacion()">Guardar Turno</button>
</div>
```

---

## 2️⃣ Guardar: POST para Crear, PUT para Actualizar

### Problema Actual
```
Frontend siempre usa POST
  ↓
❌ No actualiza si ya existe asignación
```

### Solución

**Función para Guardar**

```typescript
guardarAsignacion() {
  const asignacionData = {
    trabajadorId: this.trabajadorId,
    turnoId: this.formData.turnoId,
    horarioTurnoId: this.formData.horarioTurnoId,
    fechaInicioVigencia: this.formData.fechaInicioVigencia,
    fechaFinVigencia: this.formData.fechaFinVigencia || null
  };

  // ← LÓGICA CLAVE
  if (this.currentAsignacionId) {
    // ACTUALIZAR: Ya existe asignación
    this.asignacionService.update(
      this.currentAsignacionId,
      asignacionData
    ).subscribe({
      next: () => {
        this.showMessage('Turno actualizado correctamente');
        this.nextStep();
      },
      error: (err) => this.handleError(err)
    });
  } else {
    // CREAR: Primera asignación
    this.asignacionService.create(asignacionData)
      .subscribe({
        next: (result) => {
          this.currentAsignacionId = result.id; // ← Guardar para futuras ediciones
          this.showMessage('Turno asignado correctamente');
          this.nextStep();
        },
        error: (err) => this.handleError(err)
      });
  }
}
```

**Servicio Angular**

```typescript
// asignacion.service.ts
create(data: AsignacionTurnoCreateDto): Observable<AsignacionTurno> {
  return this.http.post<AsignacionTurno>(
    `${this.apiUrl}/AsignacionTurno`,
    data
  );
}

update(id: number, data: AsignacionTurnoUpdateDto): Observable<void> {
  return this.http.put<void>(
    `${this.apiUrl}/AsignacionTurno/${id}`,
    data
  );
}
```

**Tipos TypeScript**

```typescript
// DTOs
export interface AsignacionTurnoCreateDto {
  trabajadorId: number;
  turnoId: number;
  horarioTurnoId: number;
  fechaInicioVigencia: string; // YYYY-MM-DD
  fechaFinVigencia?: string;   // YYYY-MM-DD
}

export interface AsignacionTurnoUpdateDto 
  extends AsignacionTurnoCreateDto {
  // Mismo formato, solo que con ID en la ruta
}

export interface AsignacionTurno 
  extends AsignacionTurnoCreateDto {
  id: number;
  esVigente: boolean;
  createdAt: string;
}
```

---

## 3️⃣ Horarios Disponibles: Elegir Endpoint

### Opción A: Endpoint Específico (Recomendado para Rotativo)

**Para ROTATIVOS (ProgramacionSemanal)**

```typescript
// En componente de programación semanal
loadHorarios() {
  this.programacionService.getHorariosDisponibles()
    .subscribe({
      next: (response) => {
        // response: {
        //   total: 3,
        //   horarios: [
        //     { id: 1, nombre: 'Turno Mañana 9-5', turnoId: 1, ... }
        //   ]
        // }
        this.horarios = response.horarios;
      }
    });
}
```

**Servicio**

```typescript
getHorariosDisponibles(): Observable<{total: number, horarios: Horario[]}> {
  return this.http.get<any>(
    `${this.apiUrl}/ProgramacionSemanal/horarios-disponibles`
  );
}
```

**Template**

```html
<select formControlName="idHorarioTurno">
  <option [value]="null">-- Selecciona Horario --</option>
  <option *ngFor="let h of horarios" [value]="h.id">
    {{ h.nombre }}
  </option>
</select>
```

---

### Opción B: Catálogo General + Filtro en Cliente (Para Fijos)

**Para FIJOS (AsignacionTurno)**

```typescript
// En wizard paso 3
turnoSeleccionado: Turno;
horarios: Horario[] = [];
horariosFiltrados: Horario[] = [];

onTurnoChange(event: any) {
  const turnoId = event.target.value;
  
  // Cargar todos los horarios (si no están ya cargados)
  if (!this.horarios.length) {
    this.horarioService.getAll()
      .subscribe({
        next: (horarios) => {
          this.horarios = horarios;
          this.filtrarHorarios(turnoId);
        }
      });
  } else {
    this.filtrarHorarios(turnoId);
  }
}

filtrarHorarios(turnoId: number) {
  // Filtrar por turno seleccionado
  this.horariosFiltrados = this.horarios
    .filter(h => h.turnoId === turnoId && h.esActivo);
}
```

**Servicio**

```typescript
getAll(): Observable<Horario[]> {
  return this.http.get<Horario[]>(
    `${this.apiUrl}/HorarioTurno`
  );
}
```

---

## 📋 Checklist Completo

```javascript
WIZARD - MODO EDITAR:
□ Detectar isEditMode en ngOnInit
□ Llamar getTurnoVigente(trabajadorId)
□ Precargar turnoId, asignacionId, vigencia
□ Mostrar valores en formulario

PASO 3 - GUARDAR:
□ Validar que turnoId está seleccionado
□ Si asignacionId existe → PUT
□ Si NO existe → POST
□ Guardar asignacionId retornado para futuras ediciones

HORARIOS - SELECTORES:
□ ROTATIVO: GET .../horarios-disponibles
□ FIJO: GET .../HorarioTurno + filtrar por turnoId
□ Mostrar solo horarios activos (esActivo === true)
□ Desactivar selector si no hay horarios

ERRORES:
□ Sin turno vigente → Mostrar "Primera asignación"
□ POST falla → Mostrar error de validación
□ PUT falla → Mostrar error de actualización
□ Horarios vacíos → Mostrar "Sin horarios disponibles"
```

---

## 🔄 Flujo Completo

```
USUARIO ABRE EDITAR TRABAJADOR
    ↓
[1] Cargar trabajador
    GET /api/Rrhh/Trabajadores/{id}
    ↓
[2] Cargar turno vigente (EDITAR MODE)
    GET /api/trabajadores/{id}/turno-vigente
    ↓
    ├─ SI existe → Precargar en formulario
    └─ SI NO existe → Campo vacío (primera asignación)
    ↓
[3] Usuario selecciona turno
    ↓ onTurnoChange()
[4] Filtrar y mostrar horarios
    GET /api/Rrhh/HorarioTurno (o específico)
    ↓
[5] Usuario completa formulario
    ├─ Turno
    ├─ Horario
    └─ Vigencia
    ↓
[6] Usuario da GUARDAR
    ↓
    ├─ SI asignacionId existe → PUT /api/Rrhh/AsignacionTurno/{id}
    └─ SI NO existe → POST /api/Rrhh/AsignacionTurno
    ↓
[7] Éxito → Next Step (Paso 4)
    Error → Mostrar mensaje de error
```

---

## 📚 Recursos Útiles

**Archivos Backend Relevantes:**
- `Controllers/TrabajadoresController.cs` - getTurnoVigente
- `Controllers/AsignacionTurnoController.cs` - POST/PUT
- `Controllers/ProgramacionSemanalController.cs` - horarios-disponibles

**DTOs:**
- `AsignacionTurnoCreateDto` - Para POST
- `AsignacionTurnoUpdateDto` - Para PUT
- `ProgramacionTurnoSemanalBulkCreateDto` - Para programación semanal

**Endpoints Clave:**
```
GET    /api/trabajadores/{id}/turno-vigente
GET    /api/Rrhh/AsignacionTurno
POST   /api/Rrhh/AsignacionTurno
PUT    /api/Rrhh/AsignacionTurno/{id}
GET    /api/Rrhh/HorarioTurno
GET    /api/Rrhh/ProgramacionSemanal/horarios-disponibles
```

---

**Implementa estos 3 puntos en tu frontend y el sistema funcionará perfectamente.** ✅
