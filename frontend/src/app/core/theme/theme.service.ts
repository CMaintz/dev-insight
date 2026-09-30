import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { readStoredValue, storeValueIfPossible } from '../../shared/util/browser-storage';

export type ThemeChoice = 'system' | 'light' | 'dark';
export type EffectiveTheme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'devinsight.theme';

function readStoredChoice(): ThemeChoice {
  const stored = readStoredValue(THEME_STORAGE_KEY);
  return stored === 'light' || stored === 'dark' ? stored : 'system';
}

function persistChoice(choice: ThemeChoice): void {
  storeValueIfPossible(THEME_STORAGE_KEY, choice === 'system' ? null : choice);
}

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

  toggle(): void {
    this.choice.set(this.effective() === 'dark' ? 'light' : 'dark');
  }
}
