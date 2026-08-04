# Trabajo Práctico Integrador — Sistema de Turnos Médicos

**Desarrollo de Software 2026 — UTN FRT**

API RESTful para la gestión integral de turnos médicos: administración de especialidades, médicos y disponibilidades horarias, y autogestión de reservas y cancelaciones por parte de los pacientes.

## Integrantes

| Apellido y Nombre | Legajo |
|---|---|
| Calliari, Fabrizio | 60233 |
| Martinez Soria, Franco German | 60224 |
| Ruiz, Gonzalo German | 60467 |
| Rusconi, Mateo | 56559 |

---

## Arquitectura

Solución organizada en capas según el patrón *Layers*:

| Proyecto | Responsabilidad |
|---|---|
| `Dsw2026Tpi.Api` | Controllers, middlewares, configuración y composición de dependencias |
| `Dsw2026Tpi.Application` | Casos de uso (services), DTOs, validators y mappers |
| `Dsw2026Tpi.Domain` | Entidades, reglas de negocio e interfaces de persistencia |
| `Dsw2026Tpi.Data` | Implementación de persistencia con EF Core, configuraciones y migraciones |
| `Dsw2026Tpi.CrossCutting` | Excepciones, códigos de error, modelos y helpers transversales |

La dependencia apunta siempre hacia el dominio: `Api → Application → Domain`, con `Data` implementando las interfaces que define `Domain`.

---

## Requisitos previos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) o superior
- **SQL Server LocalDB** (incluido en Visual Studio con la carga de trabajo *Almacenamiento y procesamiento de datos*) o una instancia de SQL Server
- Herramienta de EF Core, si no la tenés instalada:

```bash
dotnet tool install --global dotnet-ef
```

---

## Configuración

### 1. Clonar el repositorio

```bash
git clone https://github.com/Franco704/dsw2026-tpi.git
```

### 2. Cadena de conexión

Está definida en `Dsw2026Tpi.Api/appsettings.Development.json`. Por defecto apunta a LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Database=Dsw2026Tpi;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True"
}
```

Si usás otra instancia de SQL Server, modificá ese valor.

### 3. Contraseña del administrador inicial

El sistema crea automáticamente el usuario administrador al iniciar por primera vez. El email se lee de `appsettings.json` (`InitialAdmin:Email`, por defecto `admin@system.com`), pero **la contraseña no se versiona**: se carga mediante *User Secrets*.

```bash
dotnet user-secrets set "InitialAdmin:Password" "TuPassword123" --project Dsw2026Tpi.Api
```

> La contraseña debe cumplir la política de Identity: **mínimo 8 caracteres**, con al menos una minúscula, una mayúscula y un dígito. Si no configurás este secreto, la aplicación no inicia.

### 4. Base de datos

La solución utiliza **dos DbContext** (dominio e Identity). Hay que aplicar las migraciones de ambos:

```bash
dotnet ef database update --context Dsw2026TpiDbContext --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api
```

```bash
dotnet ef database update --context AuthenticationDbContext --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api
```

Los roles (`ADMINISTRADOR` y `PACIENTE`) se siembran automáticamente desde `Dsw2026Tpi.Data/Sources/roles.json`.

---

## Ejecutar el proyecto

```bash
dotnet run --project Dsw2026Tpi.Api
```

Con la aplicación corriendo en modo *Development*:

- **Swagger UI**: `https://localhost:{puerto}/swagger`
- **Health check**: `https://localhost:{puerto}/health-check`

Los logs se escriben en consola y en `Dsw2026Tpi.Api/Logs/log-{fecha}.txt`.

---

## Autenticación

Todos los endpoints requieren JWT, **excepto** los de login. El flujo es:

1. Obtener el token con el login correspondiente.
2. Enviarlo en cada request en la cabecera:

```
Authorization: Bearer {token}
```

En Swagger, usá el botón **Authorize** y pegá el token.

Existen dos roles con permisos diferenciados: `ADMINISTRADOR` y `PACIENTE`.

---

## Endpoints

### Autenticación

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `POST` | `/api/auth/admin/login` | Anónimo | Login de administrador. Devuelve token y rol |
| `POST` | `/api/auth/patient/login` | Anónimo | Login de paciente. Si no existe, lo registra automáticamente |
| `POST` | `/api/auth/admin/register` | Anónimo | Alta de administradores para pruebas (provisto por la cátedra) |

**Login administrador** — `POST /api/auth/admin/login`
```json
{ "email": "admin@system.com", "password": "TuPassword123" }
```
Validaciones: `email` obligatorio y con formato válido; `password` obligatorio (mínimo 8 caracteres).

**Login paciente** — `POST /api/auth/patient/login`
```json
{ "email": "paciente@mail.com", "dni": 40123456 }
```
Validaciones: `email` obligatorio y válido; `dni` obligatorio (7 u 8 dígitos). En el primer acceso se crea el paciente y se le asigna el rol `PACIENTE`.

Respuesta de ambos:
```json
{ "token": "jwt-token", "role": "ADMINISTRADOR" }
```

### Especialidades — rol `ADMINISTRADOR`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/specialties?pageSize={n}&pageIndex={n}&name={string}` | Listado paginado, con filtro opcional por nombre |
| `POST` | `/api/specialties` | Crea una especialidad |
| `PUT` | `/api/specialties/{id}` | Actualiza una especialidad existente |
| `DELETE` | `/api/specialties/{id}` | Eliminación lógica. Devuelve `"ok"` |

**Body de POST/PUT**
```json
{ "name": "Cardiología", "description": "Especialidad del corazón" }
```
Validaciones: `name` obligatorio (3 a 100 caracteres); `description` obligatorio (10 a 100 caracteres).

**Respuesta del GET**
```json
{
  "pageSize": 10,
  "pageIndex": 1,
  "data": [ { "id": "Guid", "name": "string", "description": "string" } ],
  "total": 1
}
```

### Médicos — rol `ADMINISTRADOR`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/doctors?pageSize={n}&pageIndex={n}&name={string}` | Listado paginado, con filtro opcional por nombre |
| `GET` | `/api/doctors/{id}/availabilities` | Disponibilidad del mes en curso para el médico |
| `POST` | `/api/doctors` | Registra un médico |
| `PUT` | `/api/doctors/{id}` | Actualiza un médico existente |
| `DELETE` | `/api/doctors/{id}` | Eliminación lógica. Devuelve `"ok"` |

**Body de POST/PUT**
```json
{ "name": "Juan Pérez", "licenseNumber": "M-4021", "specialtyId": "Guid" }
```
Validaciones: `name` obligatorio (3 a 100 caracteres); `licenseNumber` obligatorio y único; `specialtyId` debe existir.

**Respuesta del GET**
```json
{
  "pageSize": 10,
  "pageIndex": 1,
  "data": [
    {
      "id": "Guid",
      "name": "string",
      "licenseNumber": "string",
      "specialty": { "id": "Guid", "name": "string" }
    }
  ],
  "total": 1
}
```

**Respuesta de `/availabilities`** — array de bloques de 30 minutos del mes actual. Devuelve vacío si nunca se cargó disponibilidad.
```json
[ { "id": "Guid", "day": "LUNES", "startTime": "09:00", "endTime": "09:30" } ]
```

### Disponibilidades — rol `ADMINISTRADOR`

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/availabilities` | Genera las disponibilidades del médico hasta fin de mes |
| `PUT` | `/api/availabilities` | Sobrescribe las disponibilidades futuras **no reservadas** |

**Body (mismo para POST y PUT)**
```json
{
  "doctorId": "Guid",
  "days": [ { "day": "LUNES", "startTime": "09:00", "endTime": "12:00" } ]
}
```

Comportamiento:
- Genera **un registro por cada bloque de 30 minutos** dentro del rango indicado, para todos los días correspondientes del resto del mes.
- `startTime` debe ser menor que `endTime` y el rango debe ser divisible en bloques de 30 minutos.
- No se permiten solapamientos de horarios para el mismo médico. Un día puede repetirse para declarar horarios partidos.
- Los **feriados y días no laborables no generan turnos**. Se cargan desde `Dsw2026Tpi.Data/Sources/Feriados.json`.

### Turnos

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `POST` | `/api/appointments` | `PACIENTE` | Reserva un turno |
| `GET` | `/api/appointments/patient?dni={n}` | `PACIENTE` | Turnos activos del paciente (excluye cancelados y atendidos) |
| `DELETE` | `/api/appointments/{id}` | `PACIENTE` | Cancela un turno. Devuelve `"ok"` |
| `GET` | `/api/appointments?date=YYYY-MM-DD` | `ADMINISTRADOR` | Turnos del día |
| `GET` | `/api/appointments/search?...` | `ADMINISTRADOR` | Búsqueda combinada paginada |

**Reservar** — `POST /api/appointments`
```json
{
  "doctorId": "Guid",
  "availabilitySlotId": "Guid",
  "patient": { "dni": 40123456 },
  "reason": "Control anual"
}
```
Validaciones: `doctorId` debe existir; `availabilitySlotId` obligatorio y correspondiente a un slot disponible; `dni` obligatorio (7 a 10 dígitos); `reason` obligatorio (mínimo 5 caracteres). No se permiten turnos en el pasado y se controla la doble reserva por concurrencia, que responde `409 Conflict`.

**Búsqueda combinada** — parámetros de query: `pageSize`, `pageIndex`, `specialtyId`, `doctorId`, `dni`, `date`. Todos opcionales.
```json
{
  "pageSize": 10,
  "pageIndex": 1,
  "data": [
    {
      "appointmentsId": "Guid",
      "appointmentsStatus": "BOOKED",
      "patient": { "dni": 40123456, "fullName": "string" },
      "doctor": {
        "doctorId": "Guid",
        "name": "string",
        "specialty": { "specialtyId": "Guid", "name": "string" }
      }
    }
  ],
  "total": 1
}
```

**Estados del turno**: `BOOKED`, `CANCELLED`, `ATTENDED`, `NO_SHOW`. Solo se puede cancelar un turno en estado `BOOKED`.

---

## Manejo de errores

Todos los errores siguen un mismo contrato, resuelto por un middleware global de excepciones:

```json
{
  "errorCode": "APPOINTMENT_CONFLICT",
  "message": "El turno no puede reservarse debido a un conflicto con la disponibilidad",
  "details": [ { "field": "availabilitySlotId", "issue": "slot_unavailable" } ]
}
```

El campo `details` es opcional y se utiliza principalmente en los errores de validación.

| Código HTTP | Situación |
|---|---|
| `400` | Error de validación de los datos de entrada |
| `401` | Falta el token, expiró o es inválido |
| `403` | El rol del usuario no habilita la operación |
| `404` | La entidad solicitada no existe |
| `409` | Conflicto de negocio (turno ya reservado, matrícula o nombre duplicado) |
| `429` | Se superó el límite de solicitudes |
| `500` | Error no controlado del servidor |

Los códigos de error están centralizados en `Dsw2026Tpi.CrossCutting/Resources/ErrorCodes.resx`.

---

## Rate limiting

Las políticas se configuran en la sección `RateLimiting` de `appsettings.json`, no están codificadas en los controladores. Las solicitudes rechazadas devuelven `429 Too Many Requests` con el formato de error general, quedan registradas en el log y **no se encolan**.

| Política | Límite | Particionado por |
|---|---|---|
| Login de administrador | 5 por minuto | Dirección IP |
| Login de paciente | 10 por minuto | Dirección IP |
| Reserva de turnos | 5 por minuto | Usuario autenticado |
| General (resto de endpoints) | 100 por minuto | Usuario autenticado o IP |

---

## Pruebas unitarias

El proyecto `PruebasUnitarias` cubre casos de uso de la capa de servicios con xUnit y NSubstitute, utilizando dobles de `IPersistence`.

```bash
dotnet test
```

---

## Convenciones generales

- **Identificadores**: todas las entidades utilizan `GUID`.
- **Eliminación lógica**: los registros no se borran físicamente, se marca el atributo `deleted`. Los eliminados no se listan.
- **Paginación**: los endpoints de listado aceptan `pageSize` y `pageIndex`, y devuelven `data` junto al `total` de registros activos.
- **RESTful**: `GET` para consultar, `POST` para crear, `PUT` para actualizar y `DELETE` para eliminación lógica.
