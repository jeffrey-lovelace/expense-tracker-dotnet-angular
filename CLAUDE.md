# CLAUDE.md

All project instructions live in `AGENTS.md`. Follow them exactly; this file only adds Claude Code specifics.

@AGENTS.md

## Claude Code specifics

- Before multi-file changes, propose a short plan and wait for approval.
- After each change run the relevant checks (`dotnet build`, `dotnet test`, `npm run lint`, `npm test`) and report results.
- TDD: show the failing test output before writing the implementation.
- Stop at the end of each step so Jeff can review before continuing.
- This file is a regular file. Never replace it with a symlink.
- At the end of every step, append an entry to the GenAI log in `GENAI.md` following `AGENTS.md` section 15.
- A `Stop` hook (`.claude/settings.json`) blocks finishing a turn while `GENAI.md` is older than the changed files. Write the entry; never bypass the hook.
