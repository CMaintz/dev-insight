import { TestBed } from '@angular/core/testing';
import { AuthTokenStore, TOKEN_STORAGE_KEY } from './auth-token.store';

describe('AuthTokenStore', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => vi.restoreAllMocks());

  it('persists, restores and clears the token', () => {
    TestBed.inject(AuthTokenStore).set('abc');
    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBe('abc');

    TestBed.resetTestingModule();
    const restored = TestBed.inject(AuthTokenStore);
    expect(restored.token()).toBe('abc');

    restored.clear();
    expect(restored.token()).toBeNull();
    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBeNull();
  });

  it('falls back to memory when storage throws', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const store = TestBed.inject(AuthTokenStore);
    expect(store.token()).toBeNull();
    store.set('xyz');
    expect(store.token()).toBe('xyz');
    store.clear();
    expect(store.token()).toBeNull();
  });
});
