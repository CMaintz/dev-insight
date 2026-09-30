import { BusyFlag } from './busy-flag';

describe('BusyFlag', () => {
  it('is active only while the work runs and returns its result', async () => {
    const flag = new BusyFlag();
    let release: (value: string) => void = () => undefined;
    const pending = flag.run(() => new Promise<string>((resolve) => (release = resolve)));
    expect(flag.active()).toBe(true);
    release('done');
    expect(await pending).toBe('done');
    expect(flag.active()).toBe(false);
  });

  it('ignores a second run while one is in flight', async () => {
    const flag = new BusyFlag();
    const first = flag.run(() => new Promise<number>((resolve) => setTimeout(() => resolve(1))));
    expect(await flag.run(() => Promise.resolve(2))).toBeUndefined();
    expect(await first).toBe(1);
  });

  it('clears itself when the work throws', async () => {
    const flag = new BusyFlag();
    await expect(flag.run(() => Promise.reject(new Error('boom')))).rejects.toThrow('boom');
    expect(flag.active()).toBe(false);
  });
});
