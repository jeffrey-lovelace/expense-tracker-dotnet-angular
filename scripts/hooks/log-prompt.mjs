// UserPromptSubmit hook: appends every prompt sent to Claude Code, unedited, to docs/ai-prompt-log.md.
//
// This is the raw history. GENAI.md is curated by hand and only links here.
// The hook never blocks a prompt and prints nothing (for this event, stdout would be added to Claude's context).
// No dependencies; runs with the same Node.js used by the Angular app, on Windows, macOS and Linux.
import { appendFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';

const LOG_FILE = join('docs', 'ai-prompt-log.md');

const HEADER = `# AI prompt log

Raw history of every prompt sent to Claude Code in this repository, written automatically by the
\`UserPromptSubmit\` hook in \`scripts/hooks/log-prompt.mjs\` (registered in \`.claude/settings.json\`).
Prompts are recorded exactly as typed, in the language they were written in, and are never edited afterwards.
The curated account of how GenAI was used, including corrections, is in [GENAI.md](../GENAI.md).

`;

function readInput() {
  try {
    return JSON.parse(readFileSync(0, 'utf8') || '{}');
  } catch {
    return {};
  }
}

// Local time with its UTC offset, e.g. 2026-10-23 14:05:12 -04:00.
function timestamp(date) {
  const pad = (n) => String(Math.trunc(Math.abs(n))).padStart(2, '0');
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ` +
    `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())} ` +
    `${sign}${pad(offset / 60)}:${pad(offset % 60)}`
  );
}

// A fence longer than any backtick run inside the prompt, so pasted code cannot close it early.
function fenceFor(text) {
  const longest = Math.max(0, ...(text.match(/`+/g) ?? []).map((run) => run.length));
  return '`'.repeat(Math.max(3, longest + 1));
}

try {
  const input = readInput();
  const prompt = typeof input.prompt === 'string' ? input.prompt.replace(/\r\n/g, '\n').trimEnd() : '';
  if (prompt.length > 0) {
    const root = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();
    const file = join(root, LOG_FILE);
    if (!existsSync(file)) {
      mkdirSync(dirname(file), { recursive: true });
      writeFileSync(file, HEADER, 'utf8');
    }

    const session = typeof input.session_id === 'string' ? input.session_id.slice(0, 8) : 'unknown';
    const fence = fenceFor(prompt);
    appendFileSync(file, `## ${timestamp(new Date())} (session ${session})\n\n${fence}text\n${prompt}\n${fence}\n\n`, 'utf8');
  }
} catch {
  // Logging must never get in the way of the prompt.
}

process.exit(0);
