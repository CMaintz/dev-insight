const LOCALE = 'en';

const compactFormatter = new Intl.NumberFormat(LOCALE, {
  notation: 'compact',
  maximumFractionDigits: 1,
});
const integerFormatter = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 0 });
const decimalFormatter = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 1 });
const dateFormatter = new Intl.DateTimeFormat(LOCALE, {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
  timeZone: 'UTC',
});
const shortDateFormatter = new Intl.DateTimeFormat(LOCALE, {
  month: 'short',
  day: 'numeric',
  timeZone: 'UTC',
});

/** 1,284 → "1.3K"; small numbers stay exact. */
export function formatCompact(value: number): string {
  return Math.abs(value) < 10_000 ? integerFormatter.format(value) : compactFormatter.format(value);
}

export function formatInteger(value: number): string {
  return integerFormatter.format(value);
}

export function formatDecimal(value: number): string {
  return decimalFormatter.format(value);
}

/** 0.4213 → "42%". */
export function formatShare(share: number): string {
  return `${Math.round(share * 100)}%`;
}

/** ISO timestamp or YYYY-MM-DD → "Sep 27, 2026" (UTC, so dates never shift a day). */
export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }
  const date = new Date(value.length === 10 ? `${value}T00:00:00Z` : value);
  return Number.isNaN(date.getTime()) ? '—' : dateFormatter.format(date);
}

export function formatShortDate(value: string): string {
  const date = new Date(value.length === 10 ? `${value}T00:00:00Z` : value);
  return Number.isNaN(date.getTime()) ? value : shortDateFormatter.format(date);
}

const DAY_MS = 86_400_000;

/** "today", "3 days ago", "5 months ago", "2 years ago"; "never" for null. */
export function formatRelative(value: string | null | undefined, now: number = Date.now()): string {
  if (!value) {
    return 'never';
  }
  const time = new Date(value).getTime();
  if (Number.isNaN(time)) {
    return 'never';
  }
  const days = Math.floor((now - time) / DAY_MS);
  if (days < 1) {
    return 'today';
  }
  if (days < 30) {
    return days === 1 ? 'yesterday' : `${days} days ago`;
  }
  const months = Math.floor(days / 30);
  if (months < 12) {
    return months === 1 ? '1 month ago' : `${months} months ago`;
  }
  const years = Math.floor(days / 365);
  return years <= 1 ? '1 year ago' : `${years} years ago`;
}

export type ScoreTone = 'good' | 'fair' | 'weak';

export interface ScoreBand {
  tone: ScoreTone;
  label: string;
}

/** Score bands used for badges and rings: ≥75 strong, ≥50 fair, else needs work. */
export function scoreBand(score: number): ScoreBand {
  if (score >= 75) {
    return { tone: 'good', label: 'Strong' };
  }
  if (score >= 50) {
    return { tone: 'fair', label: 'Fair' };
  }
  return { tone: 'weak', label: 'Needs work' };
}
