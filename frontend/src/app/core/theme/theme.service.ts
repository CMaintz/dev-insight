import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';

export type ThemeChoice = 'system' | 'light' | 'dark';
export type EffectiveTheme = 'light' | 'dark';

export const THEME_STORAGE_KEY = 'devinsight.theme';

function readStoredChoice(): ThemeChoice {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    return stored === 'light' || stored === 'dark' ? stored : 'system';
  } catch {
    return 'system';
  }
}

function persistChoice(choice: ThemeChoice): void {
  try {
    if (choice === 'system') {
      localStorage.removeItem(THEME_STORAGE_KEY);
    } else {
      localStorage.setItem(THEME_STORAGE_KEY, choice);
    }
  } catch {
    // Storage can be unavailable (private mode, blocked site data); the choice stays in memory.
  }
}

/** Light/dark theme: follows the OS unless the user picks one; stamps `data-theme` on <html>. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');
  private readonly systemDark = signal(this.media?.matches ?? false);

  readonly choice = signal<ThemeChoice>(readStoredChoice());
  readonly effective = computed<EffectiveTheme>(() => {
    const choice = this.choice();
    if (choice !== 'system') {
      return choice;
    }
    return this.systemDark() ? 'dark' : 'light';
  });

  constructor() {
    this.media?.addEventListener?.('change', (event) => this.systemDark.set(event.matches));
    effect(() => {
      const choice = this.choice();
      const root = this.document.documentElement;
      if (choice === 'system') {
        root.removeAttribute('data-theme');
      } else {
        root.setAttribute('data-theme', choice);
      }
      persistChoice(choice);
    });
  }

  /** Flips to the opposite of what is currently shown. */
  toggle(): void {
    this.choice.set(this.effective() === 'dark' ? 'light' : 'dark');
  }
}
