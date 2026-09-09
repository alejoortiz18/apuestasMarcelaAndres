---
name: swapuestas-mvc-layered-architecture
description: "Use when creating or modifying the SWApuestas ASP.NET MVC web application, .NET API, C# layers, Models, DTOs, Domain, Helpers, Constants, repositories, CRUD, SQL Server, or external data integrations."
---

# SWApuestas MVC Layered Architecture

## Purpose

Build the SWApuestas web application as the main administrative and operational MVC client, backed by the central .NET API. Keep presentation, API orchestration, business rules, persistence, and reusable utilities separate so each part remains testable and replaceable.

## Target structure

Use the closest existing solution structure. If the solution is being created, prefer this layout:

```text
src/
  SWApuestas.WebMvc/
    Controllers/
    Views/
    Models/
    ViewComponents/
    Filters/
    wwwroot/
  SWApuestas.Api/
    Controllers/
    Contracts/
    Middleware/
    Composition/
  SWApuestas.Application/
    DTOs/
      Usuarios/
      Ventas/
      Boletos/
      Loterias/
      Dispositivos/
      Soporte/
    Interfaces/
    Services/
    Mappings/
  SWApuestas.Domain/
    Entities/
    ValueObjects/
    Enums/
    Rules/
    Exceptions/
  SWApuestas.Infrastructure/
    Persistence/
      Context/
      Configurations/
      Migrations/
    Repositories/
    EntityFramework/
    SqlServer/
    Integrations/
  SWApuestas.Shared/
    Constants/
    Helpers/
    Results/
tests/
  SWApuestas.UnitTests/
  SWApuestas.IntegrationTests/
```

## Layer responsibilities

### WebMvc

- Controllers receive HTTP input, validate the request model, call an application interface, and select a view or HTTP result.
- Views use only presentation `Models` or dedicated view models.
- `Models` are not database entities and are not exposed as API contracts.
- Do not put SQL, repository calls, business rules, or non-trivial mapping in controllers or Razor views.

### Api

- Expose HTTP endpoints for the MVC client, Android applications, and approved integrations.
- API controllers receive and validate request `DTOs`, invoke an application interface, and return response `DTOs` with the correct HTTP status.
- Controllers must not access `DbContext`, `SqlConnection`, stored procedures, repositories, or external clients directly.
- Keep authentication, authorization, exception translation, model validation, correlation IDs, and common HTTP concerns in middleware or filters.
- Do not duplicate domain rules in endpoints. The API is an entry point, not the owner of business invariants.

### Application

- `DTOs` define input and output contracts between MVC/API and application use cases.
- Services coordinate use cases and transactions through interfaces.
- Keep business decisions in `Domain`; application services orchestrate them.
- Map explicitly between `Models`, `DTOs`, and `Domain` entities. Do not leak persistence entities into views.
- Define repository and external-source interfaces here, for example `IUsuarioRepository`, `IBoletoRepository`, `IVentaRepository`, and `ILotteryResultsGateway`.
- Application use cases own CRUD orchestration: validate input, load data through an interface, apply domain behavior, persist changes, and return a DTO.
- Keep API and MVC contracts stable even if SQL tables, stored procedures, or external providers change.

### Domain

- Entities model concepts such as `Usuario`, `Venta`, `Boleto`, `Loteria`, `Dispositivo`, `Resultado` and `Conversacion`.
- Domain rules own invariants: unique public code, valid ticket status transitions, operating hours, authorized device, and transactional sale completion.
- Domain code must not depend on ASP.NET MVC, Entity Framework, SQL Server, Razor, HTTP, or Android.
- Use value objects and enums when they make invalid states harder to represent.

### Infrastructure

- This is the only layer that implements CRUD against SQL Server or another external data source.
- Implement application interfaces for SQL Server, repositories, external services, QR, notifications, and device integrations. The PDA's SQLite/Lite replica is implemented by the Android client infrastructure and synchronized through the API; it is not the central authority.
- Entity Framework Core is the standard persistence mechanism for the API-to-SQL Server connection and all normal CRUD operations.
- Keep the EF Core `DbContext`, entity configurations, migrations, repositories, query implementations, transaction coordination, and stored-procedure adapters in this layer.
- Organize database code by responsibility: `Context`, `Configurations`, `Repositories`, `Queries`, `StoredProcedures`, `Migrations`, and `Transactions`.
- Register the context through dependency injection with the appropriate scoped lifetime. The composition root configures the SQL Server provider and connection string; it must not be hardcoded.
- Use EF Core LINQ for normal CRUD and projections. Use `AsNoTracking` for read-only queries, explicit includes or projections, pagination, cancellation tokens, and async methods.
- Stored procedures must be invoked through EF Core APIs such as `FromSql`, `FromSqlInterpolated`, or `ExecuteSqlInterpolated` with typed parameters and explicit result mapping. Do not create direct `SqlConnection` access in Application, Domain, MVC, or API.
- Use parameterized commands, transactions, cancellation tokens, and async APIs. Never concatenate user input into SQL.
- Persistence models and EF configurations belong here when they differ from domain entities. Do not return persistence models from API endpoints.

## Entity Framework Core rules

- Keep `DbContext` focused on persistence mapping and unit-of-work behavior; do not place business workflows in it.
- Configure relationships, keys, indexes, unique constraints, decimal precision, required fields, concurrency behavior, and delete behavior explicitly with `IEntityTypeConfiguration<T>`.
- Prefer migrations generated and reviewed from the Infrastructure project. Never modify an applied migration; create a new migration for a schema change.
- Keep database naming conventions and schema names explicit so SQL Server objects are stable across environments.
- Check `SaveChangesAsync` results and handle optimistic concurrency using a defined application result or domain exception.
- Use a transaction for multi-entity writes. Sale confirmation and ticket identifier assignment must be atomic.
- Do not expose `IQueryable` beyond the Infrastructure/application query boundary when it would leak EF implementation details.
- Do not use lazy loading by default; load relationships intentionally to avoid hidden queries.

## CRUD request flow

Every CRUD operation against SQL Server follows this direction:

```text
MVC or Android client
  -> Api Controller
  -> Request DTO and validation
  -> Application use case/service
  -> Repository or gateway interface
  -> Infrastructure implementation
  -> SQL Server, stored procedure, or external source
  -> Persistence mapping
  -> Domain/application result
  -> Response DTO
```

For reads, use dedicated query handlers or repository methods and project only the required columns. For writes, validate the command, apply domain rules, persist atomically, and return the resulting identifier and state. Do not use a generic repository when it hides important business operations or produces inefficient queries.

## SQL Server and stored procedures

- Keep table names, column names, procedure names, parameters, result sets, and transaction behavior documented in Infrastructure.
- Use explicit schemas, primary keys, foreign keys, unique constraints, indexes, and appropriate SQL Server types.
- Stored procedures must have a single clear responsibility, deterministic result contracts, `SET NOCOUNT ON`, parameterized inputs, and explicit error/transaction behavior.
- Wrap multi-step operations such as confirming a sale and assigning ticket identifiers in a SQL or application transaction with a defined isolation strategy.
- Do not place SQL Server-specific types or calls in Domain or Application interfaces unless the abstraction is intentionally a database adapter.
- Use migrations or versioned reviewed SQL scripts for every schema change.

### Shared.Constants

- Store application-wide immutable values in focused classes, for example `ValidationMessages`, `ErrorMessages`, `SuccessMessages`, `RouteNames`, and `PolicyNames`.
- Constants contain text or fixed identifiers only. They must not contain business decisions, database access, mutable state, or formatting logic.
- Prefer `const string` for compile-time literals. Use `static readonly` for values that require construction.
- Do not create one giant `Constants` class. Keep names specific and avoid duplicated messages.
- If localization becomes a requirement, move user-facing text to resource files while preserving typed message keys.

### Shared.Helpers

- Helpers contain small, stateless, broadly reusable transformations such as date formatting, QR payload normalization, pagination calculations, and safe string operations.
- Helpers must not call repositories, access HTTP context, contain business rules, or hide important side effects.
- If a helper needs domain knowledge or dependencies, move it to a domain service or application service.

## Dependency direction

```text
WebMvc -> Api -> Application -> Domain
Android -> Api -> Application -> Domain
Infrastructure -> Application and Domain
Shared <- referenced only for genuinely cross-cutting primitives
```

The Domain must remain inward-facing and framework-independent. Use dependency injection at the composition root. Depend on interfaces in the inner layers and implementations in Infrastructure.

## C# conventions

- Enable nullable reference types and treat warnings as design feedback.
- Use clear descriptive names; avoid abbreviations and one-letter variables.
- Prefer small methods with one responsibility and guard clauses for invalid input.
- Use `async`/`await` for I/O and propagate `CancellationToken`.
- Avoid static mutable state and service locator patterns.
- Use records for immutable DTOs when compatible with the project conventions.
- Validate authorization at the application boundary and enforce it again where a use case requires it.
- Never log passwords, tokens, boleto validation keys, or unnecessary personal data.

## Workflow for changes

1. Identify the use case and its domain invariants in the requirements.
2. Add or update a failing unit test for the rule or use case.
3. Update the smallest responsible layer.
4. Add explicit DTO/model mapping and validation.
5. Add integration tests for SQL Server, stored procedures, transactions, and repository behavior when touched.
6. Verify the solution, tests, migrations, and affected MVC routes before completion.

## Required quality checks

- Controllers remain thin and contain no business decisions.
- API controllers and MVC controllers cannot access SQL Server or external sources directly.
- View models, DTOs, domain entities, and persistence models are distinct where their responsibilities differ.
- Every CRUD use case has an application-level interface and an Infrastructure implementation tested against its real persistence contract.
- Every stored procedure has typed inputs, defined outputs, error handling, and a testable repository wrapper.
- Every table change has a migration or reviewed SQL script, primary and foreign keys, appropriate indexes, and a rollback strategy when applicable.
- Critical operations such as confirming a sale and assigning ticket identifiers are transactional and covered by tests.
- Do not introduce a new layer, helper, or abstraction without a concrete dependency or reuse need.