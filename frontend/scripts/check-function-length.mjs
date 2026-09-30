// Fails when a function has more than 18 lines of real code or a file is longer than 300 lines.
// "Real code" skips blank lines, comments and lines holding only brackets/punctuation.
// describe() callbacks are containers, so they are skipped; it() bodies are measured.
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Project, SyntaxKind } from 'ts-morph';

export const LIMITS = { functionLines: 18, fileLines: 300 };
export const DEFAULT_PATTERNS = ['src/**/*.ts', 'scripts/*.mjs'];

const FUNCTION_KINDS = [
  SyntaxKind.FunctionDeclaration,
  SyntaxKind.MethodDeclaration,
  SyntaxKind.ArrowFunction,
  SyntaxKind.FunctionExpression,
  SyntaxKind.Constructor,
  SyntaxKind.GetAccessor,
  SyntaxKind.SetAccessor,
];

const isCodeLine = (line) =>
  line !== '' &&
  !line.startsWith('//') &&
  !line.startsWith('*') &&
  !line.startsWith('/*') &&
  !/^[{}()[\];,]+$/.test(line);

export function countCodeLines(text) {
  return text
    .split('\n')
    .map((line) => line.trim())
    .filter(isCodeLine).length;
}

function isDescribeCallback(fn) {
  const call = fn.getParentIfKind(SyntaxKind.CallExpression);
  return Boolean(call && /^describe\b/.test(call.getExpression().getText()));
}

function functionName(fn) {
  return (
    fn.getName?.() ??
    fn.getParentIfKind(SyntaxKind.VariableDeclaration)?.getName() ??
    fn.getParentIfKind(SyntaxKind.PropertyAssignment)?.getName() ??
    fn.getParentIfKind(SyntaxKind.PropertyDeclaration)?.getName() ??
    '(anonymous)'
  );
}

function functionViolations(file, relativePath, limit) {
  return FUNCTION_KINDS.flatMap((kind) => file.getDescendantsOfKind(kind))
    .filter((fn) => fn.getBody?.() && !isDescribeCallback(fn))
    .map((fn) => ({ fn, lines: countCodeLines(fn.getBody().getText()) }))
    .filter(({ lines }) => lines > limit)
    .map(
      ({ fn, lines }) =>
        `${relativePath}:${fn.getStartLineNumber()} ${functionName(fn)} has ${lines} code lines (max ${limit})`,
    );
}

export function countFileLines(text) {
  const lines = text.split('\n').length;
  return text.endsWith('\n') ? lines - 1 : lines;
}

function fileViolation(file, relativePath, limit) {
  const lines = countFileLines(file.getFullText());
  return lines > limit ? [`${relativePath} has ${lines} lines (max ${limit})`] : [];
}

export function findViolations(root, patterns = DEFAULT_PATTERNS, limits = LIMITS) {
  const project = new Project({ compilerOptions: { allowJs: true }, useInMemoryFileSystem: false });
  project.addSourceFilesAtPaths(patterns.map((pattern) => resolve(root, pattern)));
  return project.getSourceFiles().flatMap((file) => {
    const relativePath = file.getFilePath().slice(resolve(root).replace(/\\/g, '/').length + 1);
    return [
      ...fileViolation(file, relativePath, limits.fileLines),
      ...functionViolations(file, relativePath, limits.functionLines),
    ];
  });
}

function main() {
  const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
  const violations = findViolations(root);
  violations.forEach((violation) => console.error(violation));
  if (violations.length > 0) {
    process.exitCode = 1;
  } else {
    console.log('check-function-length: all functions and files are within limits');
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main();
}
