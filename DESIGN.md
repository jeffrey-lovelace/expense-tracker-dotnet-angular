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
