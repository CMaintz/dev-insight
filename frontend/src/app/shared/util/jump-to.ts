/**
 * In-page jump for `href="#id"` links. With `<base href="/DevInsight/">` a bare fragment
 * resolves against the base, not the current route, and would navigate away — so we move
 * focus/scroll ourselves instead.
 */
export function jumpToFragment(event: Event, id: string): void {
  const target = document.getElementById(id);
  if (!target) {
    return;
  }
  event.preventDefault();
  if (!target.hasAttribute('tabindex')) {
    target.setAttribute('tabindex', '-1');
  }
  target.focus({ preventScroll: true });
  target.scrollIntoView?.({ block: 'start' });
}
