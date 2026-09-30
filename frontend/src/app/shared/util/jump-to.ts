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
