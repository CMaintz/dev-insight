import { Injectable, signal } from '@angular/core';

export type ToastKind = 'error' | 'success' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  title: string;
  detail?: string;
}

const AUTO_DISMISS_MS = 6000;
const MAX_VISIBLE = 4;

/** Minimal notification queue rendered by `ToastHost`. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly items = signal<Toast[]>([]);
  private nextId = 1;

  readonly toasts = this.items.asReadonly();

  show(kind: ToastKind, title: string, detail?: string): number {
    const id = this.nextId++;
    this.items.update((list) => [...list.slice(-(MAX_VISIBLE - 1)), { id, kind, title, detail }]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
    return id;
  }

  error(title: string, detail?: string): number {
    return this.show('error', title, detail);
  }

  success(title: string, detail?: string): number {
    return this.show('success', title, detail);
  }

  info(title: string, detail?: string): number {
    return this.show('info', title, detail);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }
}
