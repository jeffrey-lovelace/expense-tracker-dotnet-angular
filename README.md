# Expense Tracker

A registered user creates, views, edits and deletes their own expenses. Two ASP.NET Core 8 Web APIs (Identity and
Expenses, plain ADO.NET on SQLite, JWT) and an Angular 20 frontend.

- How and why it is built this way: [PRESENTATION.md](PRESENTATION.md)
- Conventions and rules: [AGENTS.md](AGENTS.md), decisions taken while building: [DESIGN.md](DESIGN.md)
- Database: [docs/database.md](docs/database.md)
- Use of GenAI: [GENAI.md](GENAI.md)

## Requirements

- .NET 8 SDK
- Node.js 20.19+ or 22.12+ and npm
- Chrome (only for the frontend unit tests)

## Run everything

```bash
cd frontend && npm ci && cd ..
node scripts/run-all.mjs
```

| App | URL |
|---|---|
| Web | http://localhost:4200 |
| Identity.Api | http://localhost:5001/swagger |
| Expenses.Api | http://localhost:5002/swagger |

Both databases (`identity.db`, `expenses.db`) are created and seeded on first start in Development. No other setup:
the development JWT signing key is in `appsettings.Development.json` and is for local use only.

## Demo credentials

| Email | Password | Data |
|---|---|---|
| `demo@ballastlane.com` | `Demo123!` | 30 sample expenses over the last 90 days |
| `other@ballastlane.com` | `Demo123!` | 3 expenses, to show that users cannot see each other's data |

In Swagger: call `POST /api/auth/login` on Identity.Api, copy `token`, then use **Authorize** on Expenses.Api.

## Run the parts separately

```bash
# backend
cd backend
dotnet run --project src/Identity/Identity.Api --launch-profile http   # http://localhost:5001
dotnet run --project src/Expenses/Expenses.Api --launch-profile http   # http://localhost:5002

# frontend
cd frontend
npm start                                                             # http://localhost:4200
```

## Tests and checks

```bash
# backend: build with zero warnings, all test projects, formatting
cd backend
dotnet build
dotnet test
dotnet format --verify-no-changes

# frontend: unit tests (headless Chrome), lint, production build
cd frontend
npm run test:ci
npm run lint
npm run build
```
