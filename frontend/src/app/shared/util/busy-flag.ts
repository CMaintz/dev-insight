import { signal } from '@angular/core';

export class BusyFlag {
  private readonly state = signal(false);

  readonly active = this.state.asReadonly();

  async run<T>(work: () => Promise<T>): Promise<T | undefined> {
    if (this.state()) {
      return undefined;
    }
    this.state.set(true);
    try {
      return await work();
    } finally {
      this.state.set(false);
    }
  }
}
