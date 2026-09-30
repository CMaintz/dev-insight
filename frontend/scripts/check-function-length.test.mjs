import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { countCodeLines, findViolations } from './check-function-length.mjs';

const LIMITS = { functionLines: 3, fileLines: 12 };

function withProject(files, run) {
  const root = mkdtempSync(join(tmpdir(), 'fn-length-'));
  try {
    mkdirSync(join(root, 'src'));
    for (const [name, content] of Object.entries(files)) {
      writeFileSync(join(root, 'src', name), content);
    }
    return run(root);
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
}

const body = (n) => Array.from({ length: n }, (_, i) => `  const v${i} = ${i};`).join('\n');

test('counts only real code lines', () => {
  const text = '{\n  // comment\n\n  const a = 1;\n  /* block */\n  return a;\n}';
  assert.equal(countCodeLines(text), 2);
});

test('flags functions over the limit and passes those within it', () => {
  const files = { 'a.ts': `function ok() {\n${body(3)}\n}\nfunction tooLong() {\n${body(4)}\n}\n` };
  const violations = withProject(files, (root) => findViolations(root, ['src/**/*.ts'], LIMITS));
  assert.equal(violations.length, 1);
  assert.match(violations[0], /src\/a\.ts:6 tooLong has 4 code lines \(max 3\)/);
});

test('skips describe() callbacks but measures it() bodies', () => {
  const spec = `describe('x', () => {\n${body(5)}\n  it('y', () => {\n${body(4)}\n  });\n});\n`;
  const violations = withProject({ 'x.spec.ts': spec }, (root) =>
    findViolations(root, ['src/**/*.ts'], { ...LIMITS, fileLines: 100 }),
  );
  assert.equal(violations.length, 1);
  assert.match(violations[0], /x\.spec\.ts:7 \(anonymous\) has 4 code lines/);
});

test('flags files over the line limit', () => {
  const violations = withProject({ 'big.ts': 'export const a = 1;\n'.repeat(13) }, (root) =>
    findViolations(root, ['src/**/*.ts'], LIMITS),
  );
  assert.deepEqual(violations, ['src/big.ts has 13 lines (max 12)']);
});
