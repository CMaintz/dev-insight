import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { aRun } from '../../../testing/fixtures';
import { AnalysisRun } from '../models/api.models';
import { RunPoller, isTerminal } from './run-poller';

describe('RunPoller', () => {
  let poller: RunPoller;
  let controller: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    poller = TestBed.inject(RunPoller);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
    vi.useRealTimers();
  });

  it('polls every interval until the run succeeds, emitting the final state', async () => {
    const seen: AnalysisRun[] = [];
    let completed = false;
    poller
      .poll('run1')
      .subscribe({ next: (r) => seen.push(r), complete: () => (completed = true) });

    await vi.advanceTimersByTimeAsync(1999);
    controller.expectNone('/api/analysis/runs/run1');

    await vi.advanceTimersByTimeAsync(1);
    controller.expectOne('/api/analysis/runs/run1').flush(aRun({ status: 'running' }));
    expect(completed).toBe(false);

    await vi.advanceTimersByTimeAsync(2000);
    controller.expectOne('/api/analysis/runs/run1').flush(aRun({ status: 'succeeded' }));

    expect(seen.map((r) => r.status)).toEqual(['running', 'succeeded']);
    expect(completed).toBe(true);
    await vi.advanceTimersByTimeAsync(10_000);
    controller.expectNone('/api/analysis/runs/run1');
  });

  it('stops on failure', async () => {
    const seen: string[] = [];
    poller.poll('run1').subscribe((r) => seen.push(r.status));
    await vi.advanceTimersByTimeAsync(2000);
    controller.expectOne('/api/analysis/runs/run1').flush(aRun({ status: 'failed', error: 'x' }));
    expect(seen).toEqual(['failed']);
  });

  it('gives up after 300 polls (~10 minutes)', async () => {
    let completed = false;
    poller.poll('run1').subscribe({ complete: () => (completed = true) });
    for (let i = 0; i < 300; i++) {
      await vi.advanceTimersByTimeAsync(2000);
      controller.expectOne('/api/analysis/runs/run1').flush(aRun({ status: 'queued' }));
    }
    expect(completed).toBe(true);
  });
});

describe('isTerminal', () => {
  it('is true only for succeeded and failed', () => {
    expect(isTerminal('succeeded')).toBe(true);
    expect(isTerminal('failed')).toBe(true);
    expect(isTerminal('queued')).toBe(false);
    expect(isTerminal('running')).toBe(false);
  });
});
