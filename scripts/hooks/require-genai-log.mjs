// Stop hook: keeps Claude Code working until GENAI.md documents the latest changes.
//
// Rule: if the working tree has changes (modified, added or untracked files that git does not ignore),
// GENAI.md must be at least as recent as the newest of them. Otherwise Claude is asked to append the
// log entry (AGENTS.md section 15) before finishing the turn.
//
// No dependencies, runs with the same Node.js used by the Angular app, works on Windows, macOS and Linux.
import { execFileSync } from 'node:child_process';
import { statSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const LOG_FILE = 'GENAI.md';

function readInput() {
  try {
    return JSON.parse(readFileSync(0, 'utf8') || '{}');
  } catch {
    return {};
  }
}

function changedFiles(root) {
  const output = execFileSync('git', ['status', '--porcelain', '-uall', '-z'], { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
  const entries = output.split('\0').filter(Boolean);
  const files = [];
  for (let i = 0; i < entries.length; i++) {
    const status = entries[i].slice(0, 2);
    const path = entries[i].slice(3);
    if (status.startsWith('R') || status.startsWith('C')) i++; // skip the original path of a rename/copy
    if (status.includes('D')) continue; // deleted files have no mtime
    files.push(path);
  }
  return files;
}

function mtime(root, file) {
  try {
    return statSync(join(root, file)).mtimeMs;
  } catch {
    return 0;
  }
}

const input = readInput();

// Second stop attempt in the same turn: never loop forever.
if (input.stop_hook_active) process.exit(0);

const root = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();

let files;
try {
  files = changedFiles(root);
} catch {
  process.exit(0); // not a git repository or git not available: do not block
}

const others = files.filter((f) => f !== LOG_FILE);
if (others.length === 0) process.exit(0);

const logTime = mtime(root, LOG_FILE);
const newer = others.filter((f) => mtime(root, f) > logTime);
if (newer.length === 0) process.exit(0);

const sample = newer.slice(0, 10).join(', ') + (newer.length > 10 ? `, and ${newer.length - 10} more` : '');
const reason =
  `${LOG_FILE} is older than files changed in this session (${sample}). ` +
  `Before finishing, append or update the entry for this step in ${LOG_FILE}, Part 2, "Session log", ` +
  `following AGENTS.md section 15: English only (translate Jeff's prompt if it was in Spanish), ` +
  `number = last entry number + 1, record what was produced, what was checked, AI mistakes found, ` +
  `and only decisions Jeff actually made. If the step is still in progress, mark it "In progress".`;

process.stdout.write(JSON.stringify({ decision: 'block', reason }));
process.exit(0);
