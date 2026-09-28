import { Resource, Signal, computed, effect, linkedSignal, untracked } from '@angular/core';

interface Reloadable {
  reload(): boolean;
}

export function reloadOn(trigger: Signal<unknown>, resource: Reloadable): void {
  let initial = true;
  effect(() => {
    trigger();
    if (initial) {
      initial = false;
      return;
    }
    untracked(() => resource.reload());
  });
}

export function stickyValue<T>(resource: Resource<T | undefined>): Signal<T | undefined> {
  return linkedSignal<T | undefined, T | undefined>({
    source: () => (resource.hasValue() ? resource.value() : undefined),
    computation: (value, previous) => value ?? (resource.error() ? undefined : previous?.value),
  });
}

export function safeValue<T>(resource: Resource<T>): Signal<T | undefined> {
  return computed(() => (resource.hasValue() ? resource.value() : undefined));
}
