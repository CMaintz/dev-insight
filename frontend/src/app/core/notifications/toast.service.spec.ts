import { TestBed } from '@angular/core/testing';
import { ToastHost } from './toast-host';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  afterEach(() => vi.useRealTimers());

  it('queues toasts, caps the list and auto-dismisses', () => {
    vi.useFakeTimers();
    const service = TestBed.inject(ToastService);
    service.error('a');
    service.success('b');
    service.info('c', 'detail');
    service.show('info', 'd');
    service.show('info', 'e');
    expect(service.toasts().map((t) => t.title)).toEqual(['b', 'c', 'd', 'e']);

    vi.advanceTimersByTime(6000);
    expect(service.toasts()).toEqual([]);
  });

  it('renders and dismisses via the host', async () => {
    const service = TestBed.inject(ToastService);
    const fixture = TestBed.createComponent(ToastHost);
    service.error('Broken', 'Details here');
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Broken');
    expect(element.querySelector('.toast--error')).not.toBeNull();

    element.querySelector<HTMLButtonElement>('button')?.click();
    await fixture.whenStable();
    expect(service.toasts()).toEqual([]);
  });
});
