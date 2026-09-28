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

export function formatCompact(value: number): string {
  return Math.abs(value) < 10_000 ? integerFormatter.format(value) : compactFormatter.format(value);
}

export function formatInteger(value: number): string {
  return integerFormatter.format(value);
}

export function formatDecimal(value: number): string {
  return decimalFormatter.format(value);
}

export function formatShare(share: number): string {
  return `${Math.round(share * 100)}%`;
}

export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }
  const date = new Date(value.length === 10 ? `${value}T00:00:00Z` : value);
  return Number.isNaN(date.getTime()) ? '—' : dateFormatter.format(date);
}

const DAY_MS = 86_400_000;

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

type ScoreTone = 'good' | 'fair' | 'weak';

export interface ScoreBand {
  tone: ScoreTone;
  label: string;
}

export function scoreBand(score: number): ScoreBand {
  if (score >= 75) {
    return { tone: 'good', label: 'Strong' };
  }
  if (score >= 50) {
    return { tone: 'fair', label: 'Fair' };
  }
  return { tone: 'weak', label: 'Needs work' };
}
