import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('follows the system by default without stamping data-theme', () => {
    const theme = TestBed.inject(ThemeService);
    TestBed.tick();
    expect(theme.choice()).toBe('system');
    expect(theme.effective()).toBe('light');
    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
  });

  it('restores a stored choice', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');
    const theme = TestBed.inject(ThemeService);
    TestBed.tick();
    expect(theme.effective()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('toggles and persists the choice', () => {
    const theme = TestBed.inject(ThemeService);
    theme.toggle();
    TestBed.tick();
    expect(theme.effective()).toBe('dark');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');

    theme.toggle();
    TestBed.tick();
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('survives unavailable storage', () => {
    const spy = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const set = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const theme = TestBed.inject(ThemeService);
    theme.toggle();
    expect(() => TestBed.tick()).not.toThrow();
    expect(theme.effective()).toBe('dark');
    spy.mockRestore();
    set.mockRestore();
  });
});
