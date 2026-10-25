# GenAI

This document has two parts:

1. **Part 1** answers the GenAI section of the exercise: generating a RESTful API for a task management system.
2. **Part 2** documents how I used GenAI tools while building this Expense Tracker, with a session log that was
   updated at the end of every step.

---

## Part 1: The exercise (Task Management API)

> Task: generate a RESTful API for a simple task management system. CRUD for tasks; each task has `title`,
> `description`, `status` and `due_date`; tasks belong to a user (a basic User model exists).

### 1.1 Tool and context

Claude Code (model: Claude Opus 5.5), in a fresh session in a separate folder with no rules file
(no `AGENTS.md`/`CLAUDE.md`). All the context the AI had was in the prompt: the stack, the entity and its rules,
the expected HTTP behavior, the security requirement and the forbidden libraries. This shows what one
self-contained prompt produces without the project rules this repository relies on.

### 1.2 The prompt

```text
You are a senior .NET engineer. Generate a RESTful API for a task management system.

Stack: .NET 8, ASP.NET Core Web API with controllers, Clean Architecture (Domain, Application, Infrastructure, Api), xUnit tests.
Entity: TaskItem { Id (Guid), Title (required, max 200), Description (optional, max 2000), Status (enum: Todo, InProgress, Done), DueDate (date, cannot be in the past on create), UserId (Guid, references an existing User) }.

Requirements:
- CRUD endpoints under /api/tasks with correct HTTP verbs and status codes (201 + Location on create, 204 on delete, 404 when not found, 400 with ProblemDetails on validation errors).
- JWT authentication: a user can only see and modify their own tasks. UserId comes from the token claims, never from the request body.
- Repository interface in Application, implementation in Infrastructure.
- Async all the way, CancellationToken in every method.
- Unit tests for the validation rules and the controller.

Do NOT use Entity Framework, Dapper or MediatR. Use ADO.NET (Microsoft.Data.Sqlite) with parameterized queries.
Output: folder structure first, then each file. Then build and run the tests.
```

### 1.3 Output (representative sample)

<!-- Jeff: the generated code, or the most relevant parts (entity, service/validation, controller, one test). -->

### 1.4 How I validated the AI's suggestions

<!-- Jeff: build, tests, reading every line, checking against the requirements, security review. -->

### 1.5 What I corrected or improved

<!-- Jeff: concrete before/after examples. -->

### 1.6 Edge cases, authentication and validation

<!-- Jeff: due_date in the past, invalid status transitions, empty title, ownership (404 vs 403), JWT, pagination limits. -->

---

## Part 2: GenAI in this project

### 2.1 Tools

- **Claude** (claude.ai, linked to my local repository) for reading the exercise, planning and writing the
  project documentation.
- **Claude Code** for implementation, test-driven, in small reviewed steps.

### 2.2 Workflow

1. **Rules first.** Before any code I wrote [AGENTS.md](AGENTS.md): constraints (no EF/Dapper/MediatR, parameterized
   SQL, no symlinks), architecture, conventions, data model and design system. `CLAUDE.md` imports it, so every session
   starts with the same context instead of me repeating it.
2. **Small steps with a stop.** Each prompt asks for one step. The agent stops, I review, then we continue.
3. **Tests as the contract.** For business rules the agent writes the failing test first; I check that the test
   actually expresses the rule before accepting the implementation.
4. **Log everything.** Every prompt is recorded unedited in [docs/ai-prompt-log.md](docs/ai-prompt-log.md) by a
   Claude Code `UserPromptSubmit` hook (`scripts/hooks/log-prompt.mjs`), so the raw history does not depend on memory.
   This document keeps the curated part: what I accepted, corrected or rejected.

### 2.3 What I watch for in AI output

- Forbidden libraries or patterns slipping in (EF Core, Dapper, MediatR, generic repositories).
- SQL built with string interpolation instead of parameters.
- Missing ownership filters (`WHERE UserId = @userId`).
- `Date.toISOString()` used for calendar dates (shifts the day in UTC-4).
- Tests that pass without asserting the rule, or that test the mock instead of the code.
- Default-looking Material UI, emojis, console warnings.
- Outdated framework versions or APIs suggested from training data.

### 2.4 Session log

### 01. Read the exercise and plan the work  (2026-10-22, Claude, no branch)

**Prompt (Jeff, translated from Spanish):**
> Can you read the PDF in the context? What do we need to do?

The project context I had written beforehand already defined the domain (Expense Tracker), the stack
(.NET Web API, ADO.NET with SQLite, Angular), the two APIs, the test tools and the root documents.
I also pasted the recruiter's email (public single repo, `GENAI.md` at root, no symlinks, deadline).

**What the AI produced:** A summary of the requirements from the PDF and the email, gaps in my plan
(a presentation file was explicitly requested; the GenAI part should include real evidence), and a schedule until Sunday.

**What was checked:** I compared the summary against the PDF and the email.

**Accepted / corrected / rejected:** Accepted the gap analysis and added `PRESENTATION.md` to the plan.

**Commits:** none (planning only, no files)

### 02. Write the agent rules and the presentation  (2026-10-22, Claude, branch `develop`)

**Prompt (Jeff, translated from Spanish):**
> Let's create the markdown files first, and the presentation.
> Take into account: Angular, Material. How will the database work? The web design must not be generic. Do not use emojis.
> Project instructions and tech stack. Git workflow: branch naming convention with examples, default branches develop and master.
> How to run the projects. ESLint. Env files. Security. The data model: how it will work and what it is. Maybe Docker.
> Design patterns. Design: the colors we will use, skeletons, typography.
> Use components, pages, auth, services, pipes; organize everything well; state, auth, middleware, dialogs.

**What the AI produced:** `AGENTS.md`, `CLAUDE.md` (regular file importing `AGENTS.md`), `PRESENTATION.md`.
Proposed two SQLite databases (one per API), money stored as integer cents, calendar dates stored as `yyyy-MM-dd`,
and a "modern ledger" visual direction. It also flagged that .NET 8 reaches end of support on Nov 10, 2026 and that
Angular 20 is no longer the latest major version.

**What was checked:** Scanned all files for emojis (none). Reviewed structure against my requirements list.

**Accepted / corrected / rejected:** Accepted after review. Two corrections from me:
I asked for `GENAI.md` to be created now and fed after each step instead of written at the end, and I asked that
the whole file be in English, with my Spanish prompts translated. The rules were updated (AGENTS.md section 15).

**Commits:** `056a2b6`

### 03. Add engineering principles to the agent rules  (2026-10-22, Claude, branch `develop`)

**Prompt (Jeff, translated from Spanish):**
> For the backend use the design patterns adapter, singleton and factory.
> Do not forget SOLID principles, OOP, value types and reference types.
> Take care of the garbage collector. Use async/await correctly. Avoid leaks.

**What the AI produced:** Rewrote `AGENTS.md` section 10 with the exact place of each pattern (Adapter: repositories,
password hasher and JWT service; Singleton: DI lifetimes for stateless services; Factory: connection factory and
entity factory methods), SOLID and OOP mapped to concrete classes, a value vs reference type table, and rules for
memory, async/await and leak prevention. Added a summary to `PRESENTATION.md` section 3.

**What was checked:** Consistency with the rest of `AGENTS.md` (sections 3, 9 and 13).

**Accepted / corrected / rejected:** Accepted after review.

**Commits:** `056a2b6`

### 04. Generate the database schema  (2026-10-22, Claude, branch `develop`)

**Prompt (Jeff, translated from Spanish):**
> Let's generate the database schema.

**What the AI produced:** Versioned migration scripts `0001_create_users.sql` (identity.db) and
`0001_create_expenses.sql` (expenses.db) with `STRICT` tables, `CHECK` constraints and indexes, plus
`docs/database.md` (diagram, decisions, migration approach, the parameterized queries, seed data). Renamed the
column `Date` to `ExpenseDate`. Added a second seeded user to demonstrate data isolation.

**What was checked:** Ran both scripts against SQLite 3.37 with valid and invalid inserts, and checked the main list
query with `EXPLAIN QUERY PLAN` (uses the index, no table scan).

**AI mistake caught by the check:** The first version validated dates with `CHECK (date(ExpenseDate) IS ExpenseDate)`
and its comment claimed it rejected `2026-02-30`. The test showed SQLite accepts that value: `date()` only checks the
format, not the real calendar. Fixed with `date(ExpenseDate, '+0 days')`, which normalizes impossible dates
(2026-02-30 becomes 2026-03-02) so the comparison fails. Re-tested with leap years (2028-02-29 accepted,
2026-02-29 rejected), 2026-04-31, month 13, timestamps and other formats.

**Accepted / corrected / rejected:** Accepted after review.

**Commits:** `056a2b6`

### 05. Remove duplicated SQL from the documentation  (2026-10-22, Claude, branch `develop`)

**Prompt (Jeff, translated from Spanish):**
> Is it a good idea to have the queries there? Is it safe?

**What the AI produced:** An assessment: publishing the SQL is not a security risk (the repository is public and the
same SQL is in the code; security comes from parameters, the `UserId` filter and the JWT, not from hiding queries;
the real risks are committing `.db` files or real secrets). The problem is maintenance: two copies of the SQL can
drift. Then removed the query catalog from `docs/database.md`, kept one example (the list query) for the trade-off,
and added a rule to `AGENTS.md` that SQL lives only in the repositories.

**What was checked:** Links and references in `AGENTS.md` and `PRESENTATION.md` updated.

**Accepted / corrected / rejected:** Corrected by Jeff: he questioned keeping the queries in the docs and approved
removing them.

**Commits:** `056a2b6`

### 06. Scaffold the .NET solution and the Angular app  (2026-10-22, Claude Code, branch `chore/infra-solution-scaffold`) (translated from Spanish)

**Prompt (Jeff):**
> Create the base .NET and Angular project. Just create it.

Follow-ups during the step: "separate it into frontend/ and backend/", "create the gitignore", "run the projects",
"commit please".

**What the AI produced:** `backend/ExpenseTracker.sln` with `Shared.Auth`, Identity and Expenses (Domain, Application,
Infrastructure, Api) and seven test projects, references following the dependency rule; `Directory.Build.props`
(net8.0, warnings as errors, analyzers) and `Directory.Packages.props` (central package versions, template packages
only); APIs on ports 5001 and 5002 with Swagger; `frontend/` with Angular 20.3 (standalone, SCSS, routing, no SSR);
one root `.gitignore` and a `.gitattributes`. Template samples (WeatherForecast, Class1, UnitTest1) removed. The
existing migration scripts were moved under `backend/` unchanged.

**What was checked:** `dotnet build` 0 warnings, 0 errors. `dotnet test` runs (no tests yet). `ng build` succeeds.
Both APIs (`/swagger`) and the Angular dev server returned HTTP 200. `git add --dry-run` confirmed `bin/`, `obj/`,
`node_modules/`, `dist/` and `.angular/` are ignored.

**Accepted / corrected / rejected:** Corrected by Jeff: the AI first followed the `AGENTS.md` layout (`src/`, `tests/`
and `web/` at the root); Jeff asked to split it into `backend/` and `frontend/`. The AI pointed out that `AGENTS.md`
(layout, commands, default branch `master` vs `main`) is now out of date; not updated yet.

**Commits:** `613bf02`

### 07. Add an AI code review workflow for pull requests  (2026-10-22, Claude Code, branch `chore/infra-solution-scaffold`) (translated from Spanish)

**Prompt (Jeff):**
> Can we create the ai-code-review.yml? For automatic reviews of the PRs.

**What the AI produced:** `.github/workflows/ai-code-review.yml` using `anthropics/claude-code-action@v1` on non-draft
PRs to `develop`, `master` and `main`. The review prompt checks the diff against `AGENTS.md` (forbidden libraries,
parameterized SQL, `UserId` ownership filter, layer rules, async misuse, Angular rules, missing tests, secrets,
emojis) and posts inline comments plus one summary comment. Tools are limited to reading code and commenting.

**What was checked:** The first run on GitHub failed with `401 Unauthorized - Claude Code is not installed on this
repository`. The AI explained the cause (the action exchanges its OIDC token for a Claude GitHub App token) and gave
two fixes: install the app, or pass `github_token`. Jeff fixed the setup on GitHub and the check passed on PR #2.
The AI noted that a green check alone does not prove the summary comment was posted.

**Accepted / corrected / rejected:** Accepted by Jeff ("AI review configured and working").

**Commits:** `11b3402`

### 08. Label pull requests after the AI review  (2026-10-22, Claude Code, branch `chore/infra-solution-scaffold`) (translated from Spanish)

**Prompt (Jeff):**
> When the AI does its review, add a label.

**What the AI produced:** A workflow step that runs only after the review step succeeds, creates the `ai-reviewed`
label if missing (`gh label create --force`) and adds it to the PR with `GITHUB_TOKEN`. Added the `issues: write`
permission needed to create labels.

**What was checked:** Not verified in a workflow run during this session.

**Accepted / corrected / rejected:** Accepted after review. Merged into `develop` through PR #2 together with
entries 06 and 07.

**Commits:** `2c65b99`, merge `6ea3425`

### 09. Switch to the feature branch for issue #1  (2026-10-23, Claude Code, branch `feature/1`) (translated from Spanish)

**Prompt (Jeff):**
> Now, to develop the functionality I created an issue with the presentation (issue #1). We must switch to the
> branch feature/1.

**What the AI produced:** Found that `origin/feature/1` pointed to `056a2b6`, created before the scaffold, so
working on it as-is would have meant no projects. Since that commit is an ancestor of `develop`, it updated `develop`
to `origin/develop` and fast-forwarded `feature/1` to it. Proposed an implementation order for the issue
(Expenses domain, application, infrastructure, then Identity, APIs and frontend), tests first.

**What was checked:** `git log --graph` shows `feature/1` at `6ea3425`, same as `develop`. Nothing overwritten.

**Accepted / corrected / rejected:** Accepted after review. Jeff reminded the AI to keep this log updated, which
it had not done since entry 05.

**Commits:** none (branch not pushed yet)

### 10. Add frontend rules: RxJS, @for track, interceptors, three files per component  (2026-10-23, Claude Code, branch `feature/1`)

**Prompt (Jeff):**
> Frontend rules: use RxJS, track by in ngFor, interceptors. Work with html, scss, ts.

**What the AI produced:** Two of the rules conflicted with `AGENTS.md` ("RxJS only at the HTTP boundary", "no
`*ngFor`"), so the AI asked before editing. Jeff chose RxJS for async flows with signals for state, and `@for` with
`track` instead of `*ngFor` with `trackBy`. Updated `AGENTS.md` (stack table and section 11.2): three files per
component (`.ts`, `.html`, `.scss`, no inline templates or styles), every `@for` tracks a stable id, RxJS operators
in services and stores exposed as signals, functional interceptors registered once in order (auth, then error).
Added the same checks to the AI review prompt in `.github/workflows/ai-code-review.yml`.

**What was checked:** Re-read section 11.2 for consistency with sections 2, 10.2 and 10.8. No code affected yet.

**Accepted / corrected / rejected:** Jeff chose the RxJS scope and the loop syntax. Accepted after review.

**Commits:** `1f88fe3`

### 11. Add rules for DTOs, business rules and data validation  (2026-10-23, Claude, branch `feature/1`)

**Prompt (Jeff, partly translated from Spanish):**
> Don't forget the backend rules: use DTOs, put the business rules in the Domain, perform data validation.

**What the AI produced:** `AGENTS.md` sections 10.9 (DTO rules and the request and response DTOs of both APIs) and
10.10 (which layer validates what, the complete Domain business rules for Expense and User, and the error format:
all errors collected, returned as `ProblemDetails` keyed by camelCase field). Section 9.4 now points to 10.10 so the
rules are written in one place only. Summary added to `PRESENTATION.md` section 3.

**What was checked:** Consistency with the database constraints in `docs/database.md` and the API contract (section 9.5).

**Accepted / corrected / rejected:** Accepted after review.

**Commits:** `8f81aaf`

### 12. Expenses: failing tests for ExpenseService (TDD red)  (2026-10-23, Claude Code, branch `feature/1`)

**Prompt (Jeff):**
> Let's start TDD on the Expenses context. Follow AGENTS.md. Create the Expense entity and domain exceptions
> (ValidationException, NotFoundException), IExpenseRepository and the DTOs. Write failing unit tests first for
> ExpenseService (create rules, NotFound for missing or other users' expenses, GetAll only returns the caller's).
> Run them, show they fail, and stop. Do not implement ExpenseService yet.

**What the AI produced:** The prompt conflicted with `AGENTS.md` in two points, so the AI asked first. Jeff chose
the Result pattern over domain exceptions, and 3 to 200 characters for the description. Created in `Expenses.Domain`:
`Expense` (private constructor, `Create`/`Update` stubs, `Rehydrate` for stored rows), `ExpenseCategory`, `Error`,
`Result`/`Result<T>`. In `Expenses.Application`: `IExpenseRepository` (every read and delete takes the owner id),
`Contracts/` DTOs (no `UserId` in requests), and an `ExpenseService` whose methods throw `NotImplementedException`.
34 tests in `ExpenseServiceTests` with Moq and a hand-written `FixedTimeProvider` (no extra package).

**What was checked:** Red run: 34 failed, 0 passed, all with `NotImplementedException`. `dotnet build` 0 warnings,
`dotnet format --verify-no-changes` clean.

**AI mistakes and fixes during the step:**
- The first build failed on CA1000 (static factories on a generic type); the factories moved to the non-generic
  `Result`. CA1716 (`Error` is a VB keyword) was suppressed on that type with a justification. CA1707 was turned off
  for `tests/` only, because AGENTS.md requires `Method_Scenario_Result` names.
- Jeff updated `AGENTS.md` (sections 10.9 and 10.10) during the step. The AI re-read it and found two differences
  in its code: `Result` held a single error while factories must return all errors, and the DTOs were not in
  `Contracts/`. Both fixed, and a test for several invalid fields at once was added.
- `dotnet format` failed because the generated `.editorconfig` asked for CRLF and no final newline, contradicting
  `.gitattributes` (LF). Changed to LF with a final newline.
- The APIs started in step 06 were still running and locked the build output; the AI stopped them.

**Accepted / corrected / rejected:** Jeff chose the Result pattern and the 3 to 200 rule. Accepted after review.
The rules were later moved into the Domain; plain `decimal` and `Guid` were kept.

**Commits:** `8f81aaf`

### 13. Enforce the GenAI log with a Claude Code hook  (2026-10-23, Claude, branch `feature/1`)

**Prompt (Jeff, translated from Spanish):**
> What I do in Claude Code does not show up in GENAI.md.

**What the AI produced:** Diagnosis: the log depended on the agent remembering a rule in `AGENTS.md`, which is not
reliable (Jeff already had to remind it once, see entry 09), and two agents writing at the same time produced two
entries numbered 11 (renumbered the Claude Code one to 12). Added a project `Stop` hook in `.claude/settings.json`
that runs `scripts/hooks/require-genai-log.mjs` (Node.js, no dependencies, works on Windows). When Claude Code tries to
finish a turn, the script compares the modification time of `GENAI.md` with every changed or untracked file reported
by `git status`; if any is newer, it returns `{"decision": "block"}` with instructions to write the entry. It never
blocks twice in the same turn (`stop_hook_active`) and does nothing outside a git repository. Updated `AGENTS.md`
section 15 and `CLAUDE.md`.

**What was checked:** Ran the script in a temporary git repository: clean tree (allowed), code changed after the log
(blocked), second stop in the same turn (allowed), log updated after the code (allowed), new untracked file after
the log (blocked), not a git repository (allowed, no output). Not yet verified inside a real Claude Code session on Windows.

**AI mistake caught by the check:** The first version printed git's "not a git repository" error to the console;
git's stderr is now ignored.

**Accepted / corrected / rejected:** Accepted after review.

**Commits:** `8f81aaf`

### 14. Audit the backend against the exercise requirements  (2026-10-23, Claude Code, branch `feature/1`)

**Prompt (Jeff):**
> Before moving to the frontend, audit the backend against the exercise requirements. Don't change code yet,
> report first. Run the build and the tests with coverage, answer a yes/no checklist with file references
> (forbidden libraries, parameterized SQL, two APIs with controllers, UserId from the JWT, password hashing, layer
> independence, no Expenses to Identity references, tests per layer, seed data, symlinks and secrets, CORS),
> then list what is missing by priority and stop.

**What the AI produced:** An audit report, no code changes. It arrived while the AI was in the middle of the
previous request (move the rules into `Expense.Create` and the domain tests); that work was paused after the
documentation part (removing `Money` and typed ids from `AGENTS.md` and `PRESENTATION.md`).

**What was checked:** `dotnet build` 0 warnings. `dotnet test` with coverlet: 34 tests, all failing on purpose (red
step), six test projects with no tests. Greps over `.csproj`, `Directory.Packages.props`, `src/` and tracked files.

**Finding:** The backend is a scaffold plus the Expenses domain and service contracts. Most checklist items are not
met yet because the code does not exist (no controllers, repositories, JWT, hashing, CORS or seed). The coverage
figures for empty projects (100%) and stubs (40 to 60%) are not meaningful and were reported as such.

**Accepted / corrected / rejected:** Accepted after review.

**Commits:** none (report only, no code changes)

### Later steps

Milestones A to C (backend) and F1 to F4 (frontend) are recorded in [docs/ai-prompt-log.md](docs/ai-prompt-log.md)
(every prompt, unedited) and in the commit history.
