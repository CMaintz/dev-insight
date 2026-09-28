import { Resource, Signal, computed, effect, linkedSignal, untracked } from '@angular/core';

interface Reloadable {
  reload(): boolean;
}

/**
 * Reloads `resource` every time `trigger` changes after its initial value — e.g. when an
 * analysis run settles. Call from an injection context (component field initialiser).
 */
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

/**
 * The resource's latest successful value, held while a refetch (e.g. a scope change) is in
 * flight so the page dims instead of flashing a skeleton. Cleared on error.
 */
export function stickyValue<T>(resource: Resource<T | undefined>): Signal<T | undefined> {
  return linkedSignal<T | undefined, T | undefined>({
    source: () => (resource.hasValue() ? resource.value() : undefined),
    computation: (value, previous) => value ?? (resource.error() ? undefined : previous?.value),
  });
}

/** `resource.value()` without the throw in error state: undefined unless a value is present. */
export function safeValue<T>(resource: Resource<T>): Signal<T | undefined> {
  return computed(() => (resource.hasValue() ? resource.value() : undefined));
}
