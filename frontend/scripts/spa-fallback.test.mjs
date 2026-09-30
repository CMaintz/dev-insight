import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { writeSpaFallback } from './spa-fallback.mjs';

test('copies index.html to an identical 404.html and adds .nojekyll', () => {
  const dir = mkdtempSync(join(tmpdir(), 'spa-fallback-'));
  try {
    const html = '<!doctype html><base href="/dev-insight/"><app-root></app-root>';
    writeFileSync(join(dir, 'index.html'), html);
    writeSpaFallback(dir);
    assert.equal(readFileSync(join(dir, '404.html'), 'utf8'), html);
    assert.ok(existsSync(join(dir, '.nojekyll')));
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('fails loudly when the build output is missing', () => {
  const dir = mkdtempSync(join(tmpdir(), 'spa-fallback-'));
  try {
    assert.throws(() => writeSpaFallback(dir), /No index.html/);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});
