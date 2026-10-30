import { randomBytes } from 'node:crypto';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = new URL('../', import.meta.url);
const password = randomBytes(24).toString('hex');
for (const path of ['.env', 'apps/web/.env']) {
  const target = new URL(path, root);
  if (existsSync(target)) {
    console.log(`Preserved existing ${path}`);
    continue;
  }
  const example = readFileSync(new URL(`${path}.example`, root), 'utf8');
  writeFileSync(target, example.replaceAll('replace_with_a_local_random_password', password), {
    flag: 'wx', mode: 0o600,
  });
  console.log(`Created ignored ${fileURLToPath(target)} (credentials not printed)`);
}
