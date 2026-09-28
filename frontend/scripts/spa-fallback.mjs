// GitHub Pages has no SPA fallback: unknown paths (deep links such as /DevInsight/dashboard or
// the OAuth return /DevInsight/auth/callback) are answered with 404.html. Making 404.html a copy
// of index.html lets the Angular router take over. `.nojekyll` stops Pages from running Jekyll,
// which would hide files whose names start with an underscore.
import { copyFileSync, existsSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const DEFAULT_DIST = resolve(
  fileURLToPath(new URL('..', import.meta.url)),
  'dist/devinsight/browser',
);

export function writeSpaFallback(distDir = DEFAULT_DIST) {
  const index = join(distDir, 'index.html');
  if (!existsSync(index)) {
    throw new Error(`No index.html in ${distDir} — run the build first.`);
  }
  copyFileSync(index, join(distDir, '404.html'));
  writeFileSync(join(distDir, '.nojekyll'), '');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  writeSpaFallback(process.argv[2] ? resolve(process.argv[2]) : DEFAULT_DIST);
  console.log('spa-fallback: wrote 404.html and .nojekyll');
}
