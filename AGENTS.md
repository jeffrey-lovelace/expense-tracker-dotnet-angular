# AGENTS.md

Instructions for any AI coding agent (Claude Code, Copilot, Cursor, etc.) working in this repository.
Humans should read it too: it is the single source of truth for conventions and decisions.

This is a technical interview exercise. Every line will be presented and defended in a live code
review, so clarity and justification matter more than cleverness.

---

## 1. Project Instructions

### 1.1 What we are building

**Expense Tracker**: a full-stack application where a registered user manages their own expenses.

User story:

> As a registered user, I want to create, view, edit and delete my expenses
> so that I can track where my money goes.

Scope required by the exercise:

- Two ASP.NET Core Web APIs:
  - `Identity.Api`: register, login (JWT), one authorized endpoint and one public endpoint.
  - `Expenses.Api`: CRUD over expenses, protected with `[Authorize]`.
- A data access layer, a business logic layer independent from data and API, and unit tests for all of them.
- An Angular frontend integrated with both APIs, responsive, with full CRUD.
- Seeded data and demo credentials.
- Root documents: `README.md`, `PRESENTATION.md`, `GENAI.md`, `AGENTS.md`, `CLAUDE.md`.

### 1.2 Hard constraints (non-negotiable)

| Constraint | Rule |
|---|---|
| Forbidden libraries | No Entity Framework, no Dapper, no MediatR. Data access is plain ADO.NET. |
| SQL safety | Parameterized queries only. Never build SQL with string interpolation or concatenation of user input. |
| Symlinks | No symlinks anywhere in the repository. `CLAUDE.md` is a regular file. |
| Emojis | No emojis or emoticons in code, UI, commit messages, logs or documentation. |
| GenAI | `GENAI.md` must exist at the repository root. Agents append to its log after every step (section 15). Part 1 (the exercise answer) is written by Jeff. |
| Repository | One public GitHub repository. Default branch `master`. |
| Deadline | Sunday Oct 25, 16:00 Bolivia time (UTC-4). After submission, no more commits until told otherwise. |
| Warnings | Zero build warnings in .NET, zero errors/warnings in the browser console. |

### 1.3 How agents must work here

1. **TDD first.** For any business rule or endpoint: write the failing test, run it and show it fails (red),
   write the minimum code to pass (green), then refactor. Commit test and implementation separately when practical
   so the history shows red then green.
2. **Small steps, then stop.** Finish one step, summarize what changed and why, and wait for review before the next one.
3. **Explain decisions.** When introducing a pattern, package or non-obvious line, add a one or two sentence
   justification in the summary (not as noisy code comments).
4. **No new packages without asking.** Use only the packages listed in section 2. Propose additions with a reason.
5. **Update the GenAI log** in `GENAI.md` at the end of every step (section 15). Do not edit Part 1 of `GENAI.md`, git history, or remote settings unless explicitly asked.
6. Code, comments, commit messages and documentation are written in **English**.
7. Prefer boring, readable code. No premature abstractions, no generic repositories, no reflection magic.

---

## 2. Tech Stack

### Backend

| Area | Choice | Notes |
|---|---|---|
| Runtime | .NET 8 (`net8.0`) | Single target framework set in `Directory.Build.props`. |
| Web | ASP.NET Core Web API with controllers | Controllers (not minimal APIs) to keep the classic MVC/Web API shape the exercise asks for. |
| Database | SQLite via `Microsoft.Data.Sqlite` | ADO.NET: `SqliteConnection`, `SqliteCommand`, parameters, `SqliteDataReader`. |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) | HS256, issuer/audience/lifetime validated. |
| Password hashing | `PasswordHasher<TUser>` from `Microsoft.Extensions.Identity.Core` | PBKDF2 with salt and versioned format. No EF dependency. |
| API docs | Swashbuckle (Swagger UI), Development only | With a Bearer token button. |
| Tests | xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory), SQLite in-memory | Assertions with xUnit `Assert`. FluentAssertions is avoided (v8 commercial license). |
| Architecture tests | NetArchTest.Rules (optional) | Asserts layer dependency rules. |
| Packages | Central Package Management (`Directory.Packages.props`) | One place for versions. |

### Frontend

| Area | Choice | Notes |
|---|---|---|
| Framework | Angular 20, standalone components only | No NgModules. |
| UI library | Angular Material 20 (Material 3) | Custom theme, see section 11. Not the default look. |
| Reactivity | RxJS for async flows + Signals for state (`signal`, `computed`, `input()`, `output()`, `model()`) | RxJS operators in services and stores; state exposed to templates as signals (`toSignal`). See section 11.2. |
| Change detection | `OnPush` on every component | Enforced by ESLint. |
| Forms | Typed Reactive Forms | Server validation errors mapped back into the form. |
| HTTP | `HttpClient` with functional interceptors | Only inside `services/`. |
| Lint/format | angular-eslint + typescript-eslint + Prettier | See section 6. |
| Tests | Angular CLI default runner (Jasmine/Karma, ChromeHeadless) | Focus on services, stores, guards, interceptors, pipes. |
| Fonts | Self-hosted via `@fontsource/*` | No external font CDN, so no network or CSP warnings. |

---

## 3. Repository Layout

```
/
  AGENTS.md  CLAUDE.md  README.md  PRESENTATION.md  GENAI.md
  ExpenseTracker.sln
  Directory.Build.props        # net8.0, Nullable, TreatWarningsAsErrors, analyzers
  Directory.Packages.props     # central package versions
  .editorconfig  .gitignore  .gitattributes
  .env.example                 # only used by docker compose (optional)
  src/
    Shared/
      Shared.Auth/             # JwtOptions, AddJwtAuthentication(), ClaimsPrincipal.GetUserId()
    Identity/
      Identity.Domain/         # User entity, Email rules
      Identity.Application/    # UserService, IUserRepository, IPasswordHasher, ITokenService, DTOs
      Identity.Infrastructure/ # SqliteUserRepository, JwtTokenService, PasswordHasher adapter, DB initializer + seed
      Identity.Api/            # AuthController, HealthController, Program.cs (composition root)
    Expenses/
      Expenses.Domain/         # Expense entity, Money, ExpenseCategory, domain errors
      Expenses.Application/    # ExpenseService, IExpenseRepository, DTOs, queries
      Expenses.Infrastructure/ # SqliteExpenseRepository, DB initializer + seed
      Expenses.Api/            # ExpensesController, CategoriesController, Program.cs
  tests/
    Identity.Application.Tests/
    Identity.Infrastructure.Tests/
    Identity.Api.Tests/
    Expenses.Domain.Tests/
    Expenses.Application.Tests/
    Expenses.Infrastructure.Tests/
    Expenses.Api.Tests/
    Architecture.Tests/        # optional, NetArchTest
  web/                         # Angular application
  docker/                      # optional Dockerfiles + nginx.conf
```

### 3.1 Dependency rule (Clean Architecture)

```
Api  ──>  Application  ──>  Domain
 │             ▲
 └──> Infrastructure ──┘   (Api references Infrastructure only to wire DI in Program.cs)
```

- `Domain` references nothing.
- `Application` references `Domain` only. It defines ports (interfaces) such as `IExpenseRepository`.
  No `Microsoft.Data.Sqlite`, no ASP.NET Core types.
- `Infrastructure` implements the ports and is the only layer that knows SQL.
- `Api` is a thin adapter: HTTP in, call the application service, map the `Result` to HTTP out.
- The two bounded contexts (Identity, Expenses) never reference each other. They share only `Shared.Auth`.

---

## 4. Git Workflow

### 4.1 Default branches

| Branch | Purpose |
|---|---|
| `master` | Submission-ready code. GitHub default branch. Only receives merges from `develop`. Tagged `v1.0.0` at submission. |
| `develop` | Integration branch. All work branches start here and merge back here. |

Never force-push to `master` or `develop`.

### 4.2 Branch naming convention

Format: `<type>/<area>-<short-description>` in kebab-case.

| Type | Use for | Example |
|---|---|---|
| `feature/` | New behavior | `feature/expenses-crud-service` |
| `feature/` | | `feature/identity-jwt-login` |
| `feature/` | | `feature/web-expense-list-page` |
| `fix/` | Bug fixes | `fix/web-date-timezone-shift` |
| `test/` | Tests only | `test/expenses-repository-sqlite` |
| `refactor/` | No behavior change | `refactor/expenses-result-mapping` |
| `docs/` | Documentation | `docs/presentation-architecture` |
| `chore/` | Tooling, config, deps | `chore/web-eslint-setup` |

Areas: `identity`, `expenses`, `shared`, `web`, `infra`, `docs`.

### 4.3 Commits

[Conventional Commits](https://www.conventionalcommits.org/): `<type>(<scope>): <imperative summary>`.

TDD pairs look like this in history:

```
test(expenses): add failing test for non-positive amount
feat(expenses): reject non-positive amounts in Expense.Create
refactor(expenses): extract amount validation into Money
```

### 4.4 Merging

- Merge work branches into `develop` with `--no-ff` (merge commit). **Do not squash**: squashing hides the
  red/green TDD evidence the reviewers want to see.
- `develop` into `master` with `--no-ff` when a milestone is stable, and once more for the final submission.

---

## 5. How to Run the Projects

### 5.1 Prerequisites

- .NET 8 SDK
- Node.js 20.19+ or 22.12+ (required by Angular 20) and npm
- Git

### 5.2 Ports

| App | URL |
|---|---|
| Identity.Api | `http://localhost:5001` (Swagger at `/swagger`) |
| Expenses.Api | `http://localhost:5002` (Swagger at `/swagger`) |
| Angular web | `http://localhost:4200` |

### 5.3 Commands

```bash
# backend
dotnet restore
dotnet build            # must produce 0 warnings
dotnet test             # all test projects

dotnet run --project src/Identity/Identity.Api
dotnet run --project src/Expenses/Expenses.Api

# frontend
cd web
npm ci
npm start               # ng serve on 4200
npm run lint
npm test -- --watch=false --browsers=ChromeHeadless
```

Databases are created and seeded automatically on first run in Development.

Demo credentials: `demo@ballastlane.com` / `Demo123!` (second user for the isolation demo: `other@ballastlane.com` / `Demo123!`)

### 5.4 Docker (optional, stretch goal)

Only after every required item is done. `docker compose up --build` runs both APIs and the Angular build
served by nginx. SQLite files live in named volumes. If it is not finished and tested, it does not ship.

---

## 6. Linting and Formatting

### Backend

- `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`,
  `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest-recommended</AnalysisLevel>`,
  `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.
- `.editorconfig` defines naming and style (file-scoped namespaces, `var` when type is obvious, `_camelCase` private fields).
- `dotnet format --verify-no-changes` must pass.

### Frontend

- `ng add @angular-eslint/schematics` (flat config `eslint.config.js`), Prettier, `eslint-config-prettier`.
- Rules turned on as errors:
  - `@angular-eslint/prefer-on-push-component-change-detection`
  - `@angular-eslint/prefer-standalone`
  - `@typescript-eslint/no-explicit-any`
  - `@typescript-eslint/explicit-function-return-type` (services and stores)
  - `no-console` (allow `console.error` only inside the global error handler)
- Scripts: `npm run lint`, `npm run format`, `npm run format:check`.

---

## 7. Environment and Configuration

### Backend

| Source | Contains | Committed |
|---|---|---|
| `appsettings.json` | Non-secret defaults: issuer, audience, token lifetime, CORS origins, connection string path | Yes |
| `appsettings.Development.json` | A development-only JWT signing key, clearly labeled, and `Seed:Enabled=true` | Yes, so reviewers can run it with zero setup |
| Environment variables | Real values: `Jwt__SigningKey`, `ConnectionStrings__Default`, `Cors__AllowedOrigins__0` | No |

Rules:

- Strongly typed options (`IOptions<JwtOptions>`) with `ValidateDataAnnotations().ValidateOnStart()`.
  The app refuses to start if the signing key is missing or shorter than 32 bytes.
- Both APIs read the same `Jwt` section (issuer, audience, key), because Expenses.Api validates tokens issued by Identity.Api.
- Outside Development, seeding is off unless `Seed__Enabled=true`.

### Frontend

- `web/src/environments/environment.ts` (production) and `environment.development.ts` (dev), file replacement in `angular.json`.
- Shape:

  ```ts
  export const environment = {
    production: false,
    identityApiUrl: 'http://localhost:5001/api',
    expensesApiUrl: 'http://localhost:5002/api',
  };
  ```

- Provided through an `InjectionToken<ApiConfig>` so services never import `environment` directly (easy to test).
- No secrets in the frontend, ever.

### Docker

- `.env.example` committed, `.env` git-ignored.

---

## 8. Security

### Authentication and authorization

- Identity.Api issues a JWT on login/register: claims `sub` (user id), `email`, `name`, `jti`. Lifetime 60 minutes.
- Validation: issuer, audience, lifetime, signing key, `ClockSkew = 30s`.
- Expenses.Api: every expense endpoint has `[Authorize]`. The user id always comes from the token (`sub`),
  never from the request body or query string.
- **Ownership**: every query filters by `UserId`. Accessing another user's expense returns **404**, not 403,
  so ids of other users cannot be enumerated.

### Credentials

- Passwords hashed with `PasswordHasher` (PBKDF2, per-user salt). Plain text never stored or logged.
- Password policy: minimum 8 characters, at least one upper case, one lower case, one digit, one symbol.
- Email normalized (trimmed, lower-case) and unique (case-insensitive index).
- Login failure message is generic: "Invalid email or password." No hint about which part failed.
- Login and register are rate limited with the built-in ASP.NET Core rate limiter (fixed window, per IP).

### API hardening

- Parameterized SQL only (constraint, see 1.2).
- Global exception handler (`IExceptionHandler`) returns RFC 7807 `ProblemDetails`. No stack traces outside Development.
- CORS: only the configured origins (`http://localhost:4200` in dev). No `AllowAnyOrigin`.
- HTTPS redirection and HSTS outside Development.
- Request DTOs validated; unknown or malformed input returns 400 with field errors.

### Frontend

- Token kept in memory with a `sessionStorage` copy so a refresh keeps the session; cleared on logout or 401.
  Trade-off documented: an httpOnly cookie is stronger against XSS but needs CSRF protection and a same-site
  setup across two APIs. For this scope, short-lived tokens plus Angular's built-in template sanitization is acceptable.
- The auth interceptor attaches the token only to our configured API URLs, never to third-party hosts.
- No `innerHTML` bindings with user data. No `bypassSecurityTrust*`.

---

## 9. Data Model

### 9.1 Storage decision

- **SQLite** with ADO.NET. It is file based, so reviewers run the app with no database server, and it is a real
  relational engine with constraints, indexes and transactions.
- **Two database files, one per API**: `identity.db` (owned by Identity.Api) and `expenses.db` (owned by Expenses.Api).
  Each service owns its data. Expenses only stores the `UserId` it reads from the JWT, there is no cross-database
  foreign key. The demo user has a fixed GUID so both seeds line up.
- Schema is versioned SQL scripts embedded in each Infrastructure project (`Persistence/Migrations/NNNN_name.sql`),
  applied at startup by a small `SqliteMigrator` that records them in a `SchemaVersions` table. Tables are `STRICT`.
  `PRAGMA journal_mode=WAL` is set by the migrator.
- Schema, diagram, decisions and seed data: [docs/database.md](docs/database.md). SQL lives only in the repositories; do not copy queries into documentation.
- Connections are created by an `ISqliteConnectionFactory` and opened per operation (`await using`). Connection pooling is handled by the provider.

### 9.2 Tables

**identity.db / Users**

| Column | Type | Rules |
|---|---|---|
| `Id` | TEXT PK | GUID string |
| `Email` | TEXT NOT NULL | `UNIQUE COLLATE NOCASE`, stored normalized |
| `DisplayName` | TEXT NOT NULL | 2 to 80 chars |
| `PasswordHash` | TEXT NOT NULL | PBKDF2 hash from `PasswordHasher` |
| `CreatedAtUtc` | TEXT NOT NULL | ISO 8601 UTC |

**expenses.db / Expenses**

| Column | Type | Rules |
|---|---|---|
| `Id` | TEXT PK | GUID string |
| `UserId` | TEXT NOT NULL | From JWT `sub`. Indexed with `ExpenseDate`. |
| `Description` | TEXT NOT NULL | 3 to 200 chars, trimmed |
| `AmountCents` | INTEGER NOT NULL | `CHECK (AmountCents BETWEEN 1 AND 100000000)`. Money stored as integer cents, never REAL. |
| `Category` | TEXT NOT NULL | `CHECK (Category IN (...))`, see list below |
| `ExpenseDate` | TEXT NOT NULL | `yyyy-MM-dd`, a calendar date with no time and no time zone. `CHECK (date(ExpenseDate, '+0 days') IS ExpenseDate)` |
| `CreatedAtUtc` | TEXT NOT NULL | ISO 8601 UTC |
| `UpdatedAtUtc` | TEXT NOT NULL | ISO 8601 UTC |

Index: `IX_Expenses_UserId_ExpenseDate ON Expenses (UserId, ExpenseDate DESC, CreatedAtUtc DESC)`.

Categories (fixed set, an enum in the Domain): `Food`, `Transport`, `Housing`, `Utilities`, `Health`,
`Entertainment`, `Shopping`, `Education`, `Other`.

### 9.3 How it works end to end

1. Angular sends `POST /api/expenses` with `{ description, amount: 12.50, category: "Food", date: "2026-10-22" }` and the Bearer token.
2. `ExpensesController` reads the user id from the token and calls `ExpenseService.CreateAsync(userId, request)`.
3. `ExpenseService` calls `Expense.Create(...)`, which enforces the business rules and returns a `Result<Expense>`.
4. On success the service calls `IExpenseRepository.AddAsync(expense)`. `SqliteExpenseRepository` converts
   `decimal` 12.50 to `1250` cents and runs a parameterized `INSERT`.
5. The controller maps `Result` to HTTP: `201 Created` with a `Location` header, or `400 ProblemDetails`.

Note: the JSON field stays `date` in the API contract; only the column is named `ExpenseDate` (clearer next to the SQL `date()` function).

### 9.4 Business rules (Domain and Application)

- Amount greater than 0, at most 1,000,000.00, at most 2 decimal places.
- Description required, 3 to 200 characters after trimming.
- Category must be one of the fixed set.
- Date required, not in the future (compared with `TimeProvider`, which tests can fake), not before 2000-01-01.
- A user can only read, update or delete their own expenses.

### 9.5 API contract

**Identity.Api** (`http://localhost:5001`)

| Verb | Route | Auth | Result |
|---|---|---|---|
| POST | `/api/auth/register` | Public | 201 + `{ token, user }`, 400 invalid, 409 email taken |
| POST | `/api/auth/login` | Public | 200 + `{ token, user }`, 401 invalid credentials |
| GET | `/api/auth/me` | Bearer | 200 + current user, 401 |
| GET | `/api/health` | Public | 200 `{ status: "Healthy" }` |

**Expenses.Api** (`http://localhost:5002`)

| Verb | Route | Auth | Result |
|---|---|---|---|
| GET | `/api/expenses?page=1&pageSize=20&category=&from=&to=` | Bearer | 200 `{ items, page, pageSize, totalCount, totalAmount }` |
| GET | `/api/expenses/{id}` | Bearer | 200, 404 |
| POST | `/api/expenses` | Bearer | 201 + Location, 400 |
| PUT | `/api/expenses/{id}` | Bearer | 200, 400, 404 |
| DELETE | `/api/expenses/{id}` | Bearer | 204, 404 |
| GET | `/api/categories` | Public | 200 list of categories |

Errors are always `ProblemDetails` (`application/problem+json`), validation errors include an `errors` dictionary keyed by field.

---

## 10. Design Patterns and Engineering Principles

### 10.1 Backend patterns

Three patterns are required by Jeff and must be easy to point at in the code review:

| Pattern | Where exactly | Why |
|---|---|---|
| **Adapter** | `SqliteExpenseRepository` and `SqliteUserRepository` adapt ADO.NET to the `IExpenseRepository` / `IUserRepository` ports. `AspNetPasswordHasher` adapts `PasswordHasher<User>` to `IPasswordHasher`. `JwtTokenService` adapts `System.IdentityModel.Tokens.Jwt` to `ITokenService`. | The Application layer depends on our interfaces, not on third-party types. Swapping SQLite or the hasher touches one class. |
| **Singleton** | Registered through DI lifetimes, never a hand-written static `Instance`: `ISqliteConnectionFactory` (stateless, holds only the connection string), `ITokenService` (holds immutable signing credentials), `IPasswordHasher`, `TimeProvider.System`. | One instance is safe because they are stateless or immutable and thread-safe. DI singletons stay replaceable in tests; a static singleton would not. |
| **Factory** | `ISqliteConnectionFactory.Create()` builds a new `SqliteConnection` per operation. Static factory methods `Expense.Create(...)`, `User.Create(...)`, `Money.Create(...)` return `Result<T>`. `ProblemDetailsFactory` usage when mapping errors to HTTP. | Construction rules in one place: an invalid entity or a misconfigured connection cannot be created. |

Also used:

| Pattern | Where | Why |
|---|---|---|
| Clean Architecture / Ports and Adapters | Whole solution | Business rules testable without database or HTTP. |
| Repository | One interface per aggregate, explicit methods | No generic repository. |
| Result pattern | `Result<T>` with typed `Error` (Validation, NotFound, Conflict, Unauthorized) | Expected failures are values, not exceptions. Mapped to HTTP in one place. |
| Value object | `Money`, strongly typed ids | Rules for amounts in one place, ids cannot be mixed up. |
| Options pattern | `JwtOptions`, `SeedOptions`, `CorsOptions` | Typed, validated configuration. |
| Dependency injection | `Program.cs` composition root | Infrastructure registered only there. |
| DTOs with manual mapping | Application layer | Explicit and readable. No AutoMapper. |
| Middleware | Global exception handler, rate limiter, CORS | Cross-cutting concerns out of controllers. |

Not used, on purpose: generic repository, Unit of Work (single-row operations), CQRS/Mediator (forbidden and
unnecessary at this size), Specification, classic static Singleton.

### 10.2 Frontend patterns

| Pattern | Where |
|---|---|
| Smart (page) and presentational components | `pages/` vs `components/` |
| Signal store (private writable signals, public readonly signals and `computed`) | `state/` |
| Adapter | API services map HTTP DTOs to frontend models |
| Singleton | Services and stores with `providedIn: 'root'` |
| Interceptor chain | `interceptors/` |
| Functional route guards | `auth/` |
| Injection tokens for configuration | `ApiConfig` |

### 10.3 SOLID, with concrete examples

| Principle | How it shows up |
|---|---|
| **S**ingle responsibility | Controller: HTTP only. Service: use case orchestration. Entity: invariants. Repository: SQL and mapping. |
| **O**pen/closed | New error kinds or repositories are added without changing existing services; `Result` to HTTP mapping is one extension method. |
| **L**iskov substitution | Any `IExpenseRepository` (SQLite, Moq fake) behaves the same from the service's point of view; tests rely on it. |
| **I**nterface segregation | Small ports: `IPasswordHasher` (2 methods), `ITokenService` (1 method), `IExpenseRepository` (only what the use cases need). |
| **D**ependency inversion | Application defines the interfaces; Infrastructure implements them; `Program.cs` wires them. |

### 10.4 OOP

- **Encapsulation**: entities have private setters and a private constructor. State changes only through methods
  (`expense.Update(...)`) that enforce the rules.
- **Abstraction**: the Application layer sees ports, not ADO.NET.
- **Polymorphism**: interface implementations (real repository vs mock), `Error` kinds handled uniformly.
- **Composition over inheritance**: no base entity class hierarchies, no base controller with logic.
  Classes are `sealed` by default; unsealing requires a reason.

### 10.5 Value types and reference types

| Kind | Type | Reason |
|---|---|---|
| `Money` | `readonly record struct` (wraps `decimal`, 16 bytes) | Small, immutable, value semantics, no heap allocation. |
| Strongly typed ids (`ExpenseId`, `UserId`) | `readonly record struct` (wraps `Guid`) | Cannot pass a `UserId` where an `ExpenseId` is expected; no allocation. |
| `Result<T>` and `Error` | `readonly record struct` / `sealed record` | Returned on every call; struct avoids an allocation per call. |
| Entities (`Expense`, `User`) | `sealed class` | They have identity and a lifecycle; reference semantics is correct. |
| Request/response DTOs | `sealed record` (class) | Immutable, value equality, serializer friendly. |

Rules:

- `default(Money)` and `default(ExpenseId)` bypass validation. Always create them through their factory; code that
  receives them from outside the domain validates them.
- Avoid boxing: do not store structs as `object` or cast them to interfaces in hot paths.
- Keep structs small (16 bytes or less) and immutable (`readonly`).
- Use `string` comparisons with an explicit `StringComparison`.

### 10.6 Memory and the garbage collector

- Every `SqliteConnection`, `SqliteCommand` and `SqliteDataReader` is disposed with `await using`.
  Connections are short-lived (open, execute, dispose); the provider pools them.
- Cache column ordinals once per reader (`reader.GetOrdinal(...)` before the loop), not per row.
- Pre-size lists when the size is known (`new List<ExpenseDto>(pageSize)`).
- SQL text lives in `private const string` fields, so it is not rebuilt per call.
- No finalizers, no large static collections, no caching of per-request data in singletons.
- Do not optimize without a reason: readability first, these rules only avoid obvious waste.

### 10.7 async/await

- Async all the way: controller, service, repository use `async Task` / `async Task<T>`. Method names end in `Async`.
- Never `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` or `async void` (deadlocks, lost exceptions).
- `CancellationToken` flows from the controller action parameter down to `ExecuteReaderAsync`, `ExecuteNonQueryAsync`
  and `ReadAsync`, so an aborted request stops the query.
- No `Task.Run` in the request path (it only moves work to another thread-pool thread).
- Do not elide `async/await` when the method has `using`/`await using` (the resource would be disposed too early).
- `ConfigureAwait(false)` is not used: ASP.NET Core has no `SynchronizationContext`, so it adds noise without benefit.
- `ValueTask` only if a measurement justifies it. Default is `Task`.

### 10.8 Avoiding leaks

Backend:

- DI lifetimes checked at startup: `ValidateScopes = true` and `ValidateOnBuild = true`, so a singleton can never
  capture a scoped dependency (captive dependency).
- Singletons are stateless or immutable. No connection, request data or user data is stored in a singleton.
- No event subscriptions from long-lived objects to short-lived ones.

Frontend:

- No manual `subscribe` in components when a signal or `toSignal` works. When needed, `takeUntilDestroyed()`.
- Timers and DOM listeners registered through `DestroyRef.onDestroy` cleanup.
- Stores in `providedIn: 'root'` hold only app state, cleared on logout.

## 11. Frontend Architecture

### 11.1 Folder structure (`web/src/app`)

```
app/
  app.config.ts             # providers: router, http + interceptors, animations, date adapter, ApiConfig
  app.routes.ts             # lazy routes with loadComponent
  auth/
    auth.store.ts           # current user, token, isAuthenticated (signals)
    token-storage.service.ts
    auth.guard.ts           # blocks private routes, redirects to /login?returnUrl=
    guest.guard.ts          # keeps logged-in users out of /login and /register
  interceptors/
    auth.interceptor.ts     # adds Bearer token to our API URLs only
    error.interceptor.ts    # 401 -> logout + redirect, 0/5xx -> snackbar, 400 passes through to the form
  services/                 # the ONLY place that injects HttpClient
    auth-api.service.ts
    expenses-api.service.ts
    categories-api.service.ts
  state/
    expenses.store.ts       # items, filters, page, totals, loading, error; load/create/update/remove
  models/                   # interfaces and types matching the API contract
  pages/                    # one folder per route; inject stores; no HttpClient
    login/  register/  expenses/  not-found/
  components/               # presentational: input()/output() only, no services injected
    app-shell/  expense-table/  expense-card-list/  expense-filters/
    expense-summary/  skeleton/  empty-state/  page-header/
  dialogs/
    expense-form-dialog/    # create and edit, returns the form value or undefined
    confirm-dialog/         # used before delete
  pipes/
    money.pipe.ts           # Intl.NumberFormat, tabular figures
    category-label.pipe.ts
  styles/                   # in src/: _tokens.scss, _theme.scss, _typography.scss
```

### 11.2 Rules

- Every component: `standalone`, `ChangeDetectionStrategy.OnPush`, `input()` / `output()` functions, `inject()`.
- **Three files per component**: `name.ts`, `name.html`, `name.scss` (plus `name.spec.ts`), linked with
  `templateUrl` and `styleUrl`. No inline `template` or `styles`. This is the Angular 20 CLI naming (no `.component` suffix).
- Templates use the built-in control flow (`@if`, `@for`, `@defer` where useful). No `*ngIf`/`*ngFor`.
- **Every `@for` has a `track` by a stable id** (`@for (expense of expenses(); track expense.id)`), never `track $index`
  for data that can be added, removed or reordered. It is the `@for` equivalent of `trackBy`: Angular reuses the DOM rows
  instead of re-rendering the whole list.
- **RxJS for async flows**: HTTP calls, debounced filters (`debounceTime`, `distinctUntilChanged`), cancelling
  stale requests (`switchMap`), error handling (`catchError`), and finalizing loading flags (`finalize`).
  This lives in services and stores. State reaches templates as signals (`toSignal` or a store signal), so no
  `async` pipe juggling and OnPush stays simple. No nested `subscribe`.
- **Interceptors** are functional (`HttpInterceptorFn`) and registered once with `provideHttpClient(withInterceptors([...]))`
  in `app.config.ts`, in this order: `authInterceptor` (adds the Bearer token to our API URLs only), then
  `errorInterceptor` (401 logout, 0/5xx snackbar, 400 passes through). Cross-cutting HTTP concerns go here, never in components or services.
- Pages talk to stores. Stores talk to API services. Components talk to nobody, they only receive inputs and emit outputs.
- No experimental Angular APIs (no `httpResource`, no signal forms) so nothing breaks during the demo.
- Subscriptions cleaned up with `takeUntilDestroyed`, or avoided by converting to signals.
- **Dates**: the API date is `yyyy-MM-dd`. Never send `Date.toISOString()` for a calendar date: in Bolivia (UTC-4) an
  expense entered at 21:00 would be saved as the next day. Convert with a small `toIsoDate()` helper that uses local date parts.
- Responsive: table at 600px and wider, card list below. Dialogs are full screen on small viewports.
- Accessibility: visible focus, labels on every field, `aria-label` on icon buttons, AA contrast.

### 11.3 Routes

| Path | Page | Guard |
|---|---|---|
| `/login` | LoginPage | guest |
| `/register` | RegisterPage | guest |
| `/expenses` | ExpensesPage | auth |
| `''` | redirect to `/expenses` | |
| `**` | NotFoundPage | |

---

## 12. Visual Design System

The goal is a product that looks designed, not an Angular Material demo. Direction: **a modern ledger**.
Warm paper background, ink-colored text, hairline rules like an accounting book, serif headlines, and amounts in a
monospaced face so columns line up. Material provides behavior and accessibility; the look comes from our tokens.

### 12.1 Color

Defined once as CSS custom properties in `_tokens.scss` and fed into the Material 3 theme
(palettes generated with `ng generate @angular/material:theme-color`).

| Token | Light | Dark | Use |
|---|---|---|---|
| `--et-paper` | `#F4F0E6` | `#121815` | Page background |
| `--et-surface` | `#FFFDF8` | `#1A221E` | Cards, dialogs, table |
| `--et-ink` | `#1B2420` | `#ECE6D8` | Primary text |
| `--et-ink-muted` | `#5E6A64` | `#A9B3AD` | Secondary text, labels |
| `--et-rule` | `#D9D2C3` | `#2C3631` | Hairlines, borders |
| `--et-primary` | `#2F5D50` (moss) | `#7FB5A3` | Primary actions, links, selected state |
| `--et-on-primary` | `#FFFFFF` | `#0E1A16` | Text on primary |
| `--et-accent` | `#C98A2B` (ochre) | `#E0A84A` | Focus rings, highlights, active filter. Never for body text (contrast). |
| `--et-danger` | `#B5462E` (vermilion) | `#E07A5F` | Delete, errors |

Category markers (a small square next to the label, never a colored chip full of color):
Food `#C98A2B`, Transport `#3F6E8C`, Housing `#7A5C3E`, Utilities `#5B7F4A`, Health `#B5462E`,
Entertainment `#7B4F7A`, Shopping `#A0673A`, Education `#2F5D50`, Other `#6F7772`.

Light and dark follow `prefers-color-scheme`. Contrast for text pairs is AA or better.

### 12.2 Typography

| Role | Font | Size / line height | Weight |
|---|---|---|---|
| Display (month total) | Fraunces | 40 / 48 | 600 |
| H1 | Fraunces | 32 / 40 | 600 |
| H2 | Fraunces | 24 / 32 | 600 |
| H3 | IBM Plex Sans | 18 / 26 | 600 |
| Body | IBM Plex Sans | 16 / 24 | 400 |
| Body small | IBM Plex Sans | 14 / 20 | 400 |
| Label / overline | IBM Plex Sans | 12 / 16, uppercase, letter-spacing 0.08em | 600 |
| Amounts, dates | IBM Plex Mono | inherits | 500, `font-variant-numeric: tabular-nums` |

Fonts are self-hosted with `@fontsource/fraunces`, `@fontsource/ibm-plex-sans`, `@fontsource/ibm-plex-mono`.
Wired into Material through the `typography` map of `mat.theme`.

### 12.3 Shape, spacing, elevation

- Spacing scale on a 4px grid: 4, 8, 12, 16, 24, 32, 48, 64.
- Corner radius small and consistent: 4px for inputs and buttons, 8px for cards and dialogs.
- Elevation replaced by 1px `--et-rule` borders. Shadows only on dialogs and menus.
- Max content width 1120px, left-aligned layout.

### 12.4 Signature elements

- **Ledger table**: hairline row separators, no zebra stripes, date in mono (`OCT 22`), description, category marker,
  amount right-aligned in mono. Row actions appear on hover/focus (always visible on touch).
- **Period header**: the total for the filtered period in Fraunces display size, with the date range as an overline.
- **Login and register**: split layout on desktop. Left panel in ink with one serif statement; right panel the form.
  Stacked on mobile.
- **Empty states**: one line of serif text, one line of explanation, one primary action. No illustrations, no emojis.

### 12.5 Loading: skeletons

- A reusable `app-skeleton` component with variants `line`, `block`, `circle` and a `width`/`height` input.
- The expense list shows 8 skeleton rows that match the real column widths, so nothing shifts when data arrives.
- The period header shows a skeleton block at display size.
- Shimmer animation 1.2s; static under `prefers-reduced-motion: reduce`.
- Buttons that submit show an inline progress state and are disabled while pending. No full-page spinners.

### 12.6 Feedback and dialogs

- `MatDialog` for create/edit (`expense-form-dialog`) and delete confirmation (`confirm-dialog`).
- `MatSnackBar` for success ("Expense saved") and for network/server errors. Field errors show inline in the form.
- Icons from Material Symbols (outlined), self-hosted.

---

## 13. Testing Strategy

| Layer | Project | What | How |
|---|---|---|---|
| Domain | `*.Domain.Tests` | Entity factories and value objects | Pure unit tests |
| Business logic | `*.Application.Tests` | Services: rules, ownership, error results | Moq for repositories, fake `TimeProvider` |
| Data access | `*.Infrastructure.Tests` | Repositories: SQL, mapping, cents conversion, user filtering | SQLite in-memory (shared cache, connection kept open per test) |
| API | `*.Api.Tests` | Status codes, routing, auth (401 without token, 404 for other user's data), ProblemDetails | `WebApplicationFactory` with in-memory SQLite and a test JWT |
| Architecture | `Architecture.Tests` (optional) | Layer dependency rules | NetArchTest |
| Frontend | `web` | Stores, API services (`HttpTestingController`), guards, interceptors, pipes | Jasmine/Karma |

Test naming: `MethodName_Scenario_ExpectedResult`, for example `Create_WithZeroAmount_ReturnsValidationError`.
Arrange / Act / Assert blocks separated by a blank line.

---

## 14. Definition of Done (per step)

- Tests written first and passing: `dotnet test` green, `npm test` green.
- `dotnet build` with 0 warnings, `npm run lint` clean.
- No forbidden libraries, no symlinks, no emojis.
- Browser console clean when the feature is used.
- Docs updated if a decision changed (`AGENTS.md`, `PRESENTATION.md`).
- A new entry appended to the GenAI log in `GENAI.md`.

---

## 15. GenAI Log (how agents feed `GENAI.md`)

At the end of every step, append one entry to **Part 2, section "Session log"** of `GENAI.md`, newest at the bottom.
The log is evidence for the interview panel, so it must be honest and verifiable.

Entry format:

```markdown
### <NN>. <short title>  (<yyyy-mm-dd>, <tool>, branch `<branch>`)

**Prompt (Jeff):**
> <Jeff's prompt in English. If Jeff wrote in Spanish, translate it faithfully to English (same meaning and level of
> detail, no additions) and add "(translated from Spanish)" after the heading.>

**What the AI produced:** <files or changes, 1 to 3 lines>

**What was checked:** <tests run and result, build/lint result, manual checks>

**Accepted / corrected / rejected:** <only what Jeff actually decided. If Jeff has not reviewed yet, write "Pending review by Jeff.">

**Commits:** <hashes or "not committed yet">
```

Rules:

- Never invent corrections, review comments or opinions on Jeff's behalf. Record only what happened in the session.
- Record the AI's own mistakes when they are found (failing test, wrong assumption, forbidden pattern caught). They are the most valuable entries.
- Everything in `GENAI.md` is written in English. Never paste Spanish text into it.
- Keep each entry short. No emojis.
- Never paste secrets or tokens into the log.
