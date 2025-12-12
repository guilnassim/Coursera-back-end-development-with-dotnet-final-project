# Coursera-back-end-development-with-dotnet-final-project

# TechHive User Management API (Minimal API, .NET 8)

**TechHive Solutions – Internal Tools**  
A clean, extensible **User Management API** built with **.NET 8 Minimal APIs** following **Clean Architecture**, **SOLID**, and **Clean Code** principles.

- **Features**: CRUD users, pagination & filtering
- **Cross‑cutting**: Centralized error handling, request/response auditing logs, JWT bearer authentication
- **DX**: Swagger/OpenAPI, Visual Studio `.http` requests, unit tests (xUnit)
- **Persistence**: In‑memory repository (swappable for EF Core later)

---

## Tech Stack

- **.NET**: .NET 8
- **API**: ASP.NET Core Minimal APIs
- **Auth**: JWT Bearer (HMAC SHA‑256)
- **Docs**: Swagger (Swashbuckle)
- **Tests**: xUnit
- **Architecture**: Clean Architecture (Domain / Application / Infrastructure / Presentation)

---

## Solution Structure

```
TechHive.UserManagement/
├─ TechHive.UserManagement.sln
├─ TechHive.UserManagement.Domain/
│  ├─ TechHive.UserManagement.Domain.csproj
│  └─ User.cs
├─ TechHive.UserManagement.Application/
│  ├─ TechHive.UserManagement.Application.csproj
│  ├─ Abstractions.cs              # IUserRepository, DTOs, PagedResult, IUserService
│  ├─ UserService.cs               # Business logic + pagination/filtering
│  └─ UserRequestValidators.cs     # static helper validators (no DI)
├─ TechHive.UserManagement.Infrastructure/
│  ├─ TechHive.UserManagement.Infrastructure.csproj
│  └─ InMemoryUserRepository.cs    # Thread-safe in-memory store
├─ TechHive.UserManagement.Tests/
│  ├─ TechHive.UserManagement.Tests.csproj
│  └─ UserServiceTests.cs
└─ UserManagementAPI/               # Presentation layer (Minimal API)
   ├─ UserManagementAPI.csproj
   ├─ appsettings.json              # JWT settings (dev)
   ├─ Program.cs                    # Endpoints + DI + Swagger + Auth + Middleware pipeline
   ├─ Middlewares/
   │  ├─ ErrorHandlingMiddleware.cs
   │  └─ RequestResponseLoggingMiddleware.cs
   └─ Tests/
      └─ UserManagementAPI.http     # Visual Studio HTTP requests
```

---

## Clean Architecture Overview

- **Domain**: Enterprise core (entities & invariants). **No dependencies**.
- **Application**: Use cases, DTOs, interfaces, services, **static validators**. Depends on Domain.
- **Infrastructure**: Adapters (repositories). Depends on Application.
- **Presentation**: Minimal API endpoints, DI, middleware, Swagger, auth. Depends on Application (+ Infrastructure for wiring).

---

## Prerequisites

- .NET 8 SDK
- (Optional) Visual Studio 2022 / VS Code
- Trusted HTTPS dev certificate:
  ```bash
  dotnet dev-certs https --trust
  ```

---

## Configuration

**`UserManagementAPI/appsettings.json`**
```json
  {
    "Jwt": {
      "Issuer": "TechHive.UserManagement",
      "Audience": "TechHive.InternalTools",
      "Secret": "TechHiveDevSecret_ChangeInProd_1234567890123456",
      "TokenLifetimeMinutes": 30
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Information"
      }
    },
    "AllowedHosts": "*"
  }
```


> **Production**: Store the `Secret` securely and rotate regularly.

---

## Build, Test, Run

```bash
# Restore & build
dotnet build

# Run unit tests
dotnet test

# Run the API (HTTPS typically https://localhost:7001)
dotnet run --project UserManagementAPI
```

### Swagger (API Explorer)
- Open: `https://localhost:7001/swagger`
- Click **Authorize** and **paste only the JWT token** (no `Bearer ` prefix—Swagger adds it).

> If Swagger UI doesn’t show, ensure it’s enabled in `Program.cs` (either always-on, or gated by `IsDevelopment()` with `ASPNETCORE_ENVIRONMENT=Development`).

---

## Authentication (JWT)

All `/api/users` endpoints require a valid **Bearer JWT**.

### Issue a token (DEV only)
The API exposes **two** endpoints to obtain a token:

1) **GET** (query)
```
GET /auth/token?subject=hr.user@techhive.local
```

2) **POST** (JSON)
```http
POST /auth/token
Content-Type: application/json

{ "subject": "hr.user@techhive.local" }
```

Response:
```json
{ "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6..." }
```

> In Swagger’s **Authorize** dialog, paste the **raw token** (no `Bearer `).  
> Issuer/Audience/Key match the API’s validation settings.

---

## Visual Studio `.http` Requests

Use the ready-made manual test collection:  
**`UserManagementAPI/Tests/UserManagementAPI.http`**

**How to use**
1. Run the API: `dotnet run --project UserManagementAPI`
2. Open the `.http` file in Visual Studio.
3. Execute **Get Token** (GET or POST), copy token, set `@token` variable.
4. Send the CRUD requests.

The file includes:
- Token issuance (GET & POST)
- List (pagination/filter)
- Create (valid & invalid)
- Get by id
- Update
- Delete
- Not found & error scenarios
- Swagger

---

## Endpoints (Quick Reference)

Base URL: `https://localhost:7001`

**Auth**
- `GET /auth/token?subject={email}` — issue token (DEV)
- `POST /auth/token` (JSON `{ "subject": "..." }`) — issue token (DEV)

**Users** *(requires Authorization: Bearer {token})*
- `GET /api/users?department=&isActive=&page=1&pageSize=20`
- `GET /api/users/{id}`
- `POST /api/users`
  ```json
  { "firstName": "Ada", "lastName": "Lovelace", "email": "ada@techhive.local", "department": "R&D", "isActive": true }
  ```
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`
- `GET /api/users/boom` — simulate an exception (for error middleware)

---

## Middleware Pipeline (Order & Behavior)

1. **ErrorHandlingMiddleware** *(first)*  
   - Catches unhandled exceptions and returns:
     ```json
     { "error": "Internal server error.", "traceId": "<id>" }
     ```
   - Logs exception with trace ID.

2. **Authentication & Authorization**  
   - Validates **JWT** tokens; returns **401** on missing/invalid tokens.

3. **RequestResponseLoggingMiddleware** *(last)*  
   - Auditing logs: **method**, **path**, **status**.
   - Adds/echoes **X-Correlation-ID**.

> Swagger is left publicly accessible; use the **Authorize** button to call protected endpoints.

---

## Validation

- Input validation uses **static helper methods** (`UserRequestValidators.cs`) that throw `ArgumentException` for invalid inputs.
- Domain entity (`User`) enforces invariants on create/update.
- For client-friendly errors, map validation exceptions to **400 Bad Request** (either in endpoints or by enhancing error middleware).

---

## Troubleshooting

- **Swagger UI not visible**: Ensure Swagger is registered; if gated by environment, run with `ASPNETCORE_ENVIRONMENT=Development` or enable Swagger always.
- **Cannot obtain token**: Use GET `/auth/token?subject=...` or POST `/auth/token` with `{ "subject": "..." }`. Ensure Issuer/Audience/Secret match.
- **401 Unauthorized**: Click **Authorize** and paste the **token only** (Swagger adds `Bearer `). Use the HTTPS URL printed at startup.
- **500 on invalid input**: Catch `ArgumentException` in endpoints or map to 400 in middleware.

---

## Roadmap

- Persistence: **EF Core** (SQLite for dev; SQL Server/PostgreSQL for prod) + migrations
- Validation: **FluentValidation** + automatic 400 mapping
- Observability: **Serilog** + Application Insights
- Security: Role-based authorization (`users.read`, `users.write`)
- API: Versioning, standardized **ProblemDetails**, rate limiting
- Tests: Integration tests (`WebApplicationFactory`), test containers

---

## License

(Choose a license, e.g., MIT.)

---

## Maintainers

- **TechHive Solutions – Internal Tools**
- Primary developer: **Guilherme Nassim Foz**

---

### Quick Start

```bash
dotnet build
dotnet test
dotnet run --project UserManagementAPI
# Open https://localhost:7001/swagger
# GET /auth/token?subject=hr.user@techhive.local → copy token → Authorize → call /api/users
```
