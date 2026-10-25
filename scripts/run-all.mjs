// Starts both APIs and the Angular dev server together, with prefixed output. Ctrl+C stops all three.
// Usage (from the repository root): node scripts/run-all.mjs
// No dependencies; the same Node.js the Angular app needs. Works on Windows, macOS and Linux.
import { spawn, spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');

const apps = [
  {
    name: 'identity',
    command: 'dotnet run --project src/Identity/Identity.Api --launch-profile http',
    cwd: join(root, 'backend'),
  },
  {
    name: 'expenses',
    command: 'dotnet run --project src/Expenses/Expenses.Api --launch-profile http',
    cwd: join(root, 'backend'),
  },
  { name: 'web     ', command: 'npm start', cwd: join(root, 'frontend') },
];

const children = apps.map(({ name, command, cwd }) => {
  const child = spawn(command, { cwd, shell: true, env: process.env });
  const write = (stream) => (chunk) => {
    for (const line of chunk.toString().split(/\r?\n/)) {
      if (line.trim().length > 0) {
        stream.write(`[${name}] ${line}\n`);
      }
    }
  };
  child.stdout.on('data', write(process.stdout));
  child.stderr.on('data', write(process.stderr));
  child.on('exit', (code) => process.stdout.write(`[${name}] exited with code ${code}\n`));
  return child;
});

// With shell: true the real process is a grandchild, so stop the whole tree.
function stopAll() {
  for (const child of children) {
    if (child.exitCode !== null || child.pid === undefined) {
      continue;
    }

    if (process.platform === 'win32') {
      spawnSync('taskkill', ['/pid', String(child.pid), '/T', '/F'], { stdio: 'ignore' });
    } else {
      child.kill('SIGTERM');
    }
  }

  process.exit(0);
}

process.on('SIGINT', stopAll);
process.on('SIGTERM', stopAll);

process.stdout.write(
  'Identity.Api  http://localhost:5001/swagger\n' +
    'Expenses.Api  http://localhost:5002/swagger\n' +
    'Web           http://localhost:4200  (demo@ballastlane.com / Demo123!)\n' +
    'Press Ctrl+C to stop all three.\n',
);
