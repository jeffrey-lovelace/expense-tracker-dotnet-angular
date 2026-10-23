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

<!-- Jeff: tool used (Claude Code, model), what context was given (AGENTS-style rules file, existing User model), and why. -->

### 1.2 The prompt

<!-- Jeff: the exact prompt, in a code block. -->

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
4. **Log everything.** Every step adds an entry below with my prompt, what the AI produced, what was checked and
   what I accepted or corrected.

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

**Commits:** not committed yet

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

**Accepted / corrected / rejected:** Pending review by Jeff. Two corrections from me so far:
I asked for `GENAI.md` to be created now and fed after each step instead of written at the end, and I asked that
the whole file be in English, with my Spanish prompts translated. The rules were updated (AGENTS.md section 15).

**Commits:** not committed yet

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

**Accepted / corrected / rejected:** Pending review by Jeff.

**Commits:** not committed yet

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

**Accepted / corrected / rejected:** Pending review by Jeff.

**Commits:** not committed yet

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

**Commits:** not committed yet

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

**Accepted / corrected / rejected:** Pending review by Jeff. Merged into `develop` through PR #2 together with
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

**Accepted / corrected / rejected:** Pending review by Jeff. Jeff reminded the AI to keep this log updated, which
it had not done since entry 05.

**Commits:** none (branch not pushed yet)
