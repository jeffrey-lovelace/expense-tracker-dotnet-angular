# Design decisions

Decisions taken during implementation that `AGENTS.md` does not cover. Each one is the simplest option that meets
the requirements; the alternative is noted so it can be discussed in the review.

## Expenses

| Decision | Why | Alternative |
|---|---|---|
| "Today" for the not-in-the-future rule is the server's local calendar day (`TimeProvider.GetLocalNow()`). The server is assumed to run in the users' time zone (Bolivia, UTC-4). | One time zone for all users in this exercise. Using UTC would reject a valid expense entered after 20:00 local time. | The client sends its time zone (or offset) and the service builds "now" from it. |
| Requests carry `Category` as a string; only exact enum names are accepted (`"Food"`, not `"food"`, `"5"` or `"Food,Transport"`). | An unknown value becomes a field error together with the other field errors, instead of a JSON binding failure. The UI only sends values it got from `GET /api/categories`. | An enum in the request DTO with `JsonStringEnumConverter` (unknown values fail model binding with a less specific message). |
| `Expense.Create` throws `ArgumentException` for an empty owner id. | The owner comes from the JWT, never from the user, so an empty id is a programming error, not an expected failure. | Return a validation error. |
| The service re-checks `expense.UserId == userId` after the repository call. | Defense in depth: a broken SQL filter still cannot leak another user's expense. Costs one comparison. | Trust the repository filter only. |
| `ListAsync` replaces "get all": `GET /api/expenses` is paged (`page`, `pageSize` 1 to 100) and filtered (`category`, `from`, `to`), and returns `totalCount` and `totalAmount` over every matching row. | AGENTS.md 9.5 defines this contract and the UI needs the period total. Without paging the list grows without limit. | Return every row and let the client page and sum. |
| `UpdateAsync` returns `bool`; 0 rows updated is NotFound. | The row can be deleted by another request between the read and the write. Returning success would report a save that never happened. | Optimistic concurrency with a row version (more than this scope needs). |

## Persistence

| Decision | Why | Alternative |
|---|---|---|
| Each Infrastructure project has its own `SqliteConnectionFactory` and `SqliteMigrator` (as `docs/database.md` describes), not a shared project. | AGENTS.md lets the two contexts share only `Shared.Auth`; each service owns its persistence. The duplicated code is small. | A `Shared.Sqlite` project used by both. |
| Migrations are embedded resources applied at startup, before the API accepts requests. | Reviewers run the API with no setup step; the schema always matches the code. | A separate migration command. |

## API

| Decision | Why | Alternative |
|---|---|---|
| `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`. | Field rules are validated in one layer, the Domain (AGENTS.md 10.10). Otherwise ASP.NET adds its own `[Required]` check with a different message and a PascalCase key (`Description`), so the form would receive two styles of error. The API still rejects malformed JSON and wrong types. | Keep the implicit check and map its keys to camelCase. |
| `UseStatusCodePages()` with `AddProblemDetails()`. | The JWT handler answers 401 with an empty body; this gives it (and unknown routes) a ProblemDetails body like every other error. | Custom `JwtBearerEvents.OnChallenge`. |
| The token-validation part of `Shared.Auth` (`JwtOptions`, `AddJwtAuthentication`, `TryGetUserId`) was built in Milestone A. | Expenses.Api needs it to protect its endpoints; issuing tokens stays in Milestone B. | Fake authentication in Expenses.Api until Identity exists. |
| CA1716 (identifier matches a Visual Basic keyword, e.g. `Error`, `Shared`) is off for the whole solution. | The solution is C# only; the names come from AGENTS.md. | Rename `Error` and `Shared.Auth`. |
