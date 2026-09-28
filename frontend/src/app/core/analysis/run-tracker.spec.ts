import { TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';
import { aRun } from '../../../testing/fixtures';
import { AnalysisApi } from '../api/analysis.api';
import { AnalysisRun } from '../models/api.models';
import { ToastService } from '../notifications/toast.service';
import { RunPoller } from './run-poller';
import { RunTracker } from './run-tracker';

describe('RunTracker', () => {
  let tracker: RunTracker;
  let polls: Map<string, Subject<AnalysisRun>>;
  let api: { run: ReturnType<typeof vi.fn>; runAll: ReturnType<typeof vi.fn> };
  let toastError: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    polls = new Map();
    toastError = vi.fn();
    api = {
      run: vi.fn((repoId: string, scope: string) =>
        of(aRun({ id: `${repoId}-${scope}`, repositoryId: repoId })),
      ),
      runAll: vi.fn(() => of([aRun({ id: 'a' }), aRun({ id: 'b', repositoryId: 'r2' })])),
    };
    TestBed.configureTestingModule({
      providers: [
        { provide: AnalysisApi, useValue: api },
        { provide: ToastService, useValue: { error: toastError } },
        {
          provide: RunPoller,
          useValue: {
            poll: (id: string) => {
              const subject = new Subject<AnalysisRun>();
              polls.set(id, subject);
              return subject.asObservable();
            },
          },
        },
      ],
    });
    tracker = TestBed.inject(RunTracker);
  });

  it('starts one run per scope and tracks them until they settle', async () => {
    await tracker.analyse('r1');

    expect(api.run).toHaveBeenCalledWith('r1', 'repo');
    expect(api.run).toHaveBeenCalledWith('r1', 'user');
    expect(tracker.isActive('r1')).toBe(true);
    expect(tracker.activeCount()).toBe(2);

    polls.get('r1-repo')?.next(aRun({ id: 'r1-repo', status: 'succeeded' }));
    polls.get('r1-user')?.next(aRun({ id: 'r1-user', status: 'running' }));
    expect(tracker.stateFor('r1').label).toBe('Analysing (1/2)');
    expect(tracker.settledCount()).toBe(1);

    polls.get('r1-user')?.next(aRun({ id: 'r1-user', status: 'succeeded' }));
    expect(tracker.stateFor('r1').phase).toBe('succeeded');
    expect(tracker.settledCount()).toBe(2);
    expect(tracker.activeCount()).toBe(0);
  });

  it('is idle for repositories without runs', () => {
    expect(tracker.stateFor('unknown')).toEqual({ phase: 'idle', label: '' });
  });

  it('shows Queued for a single queued run', async () => {
    await tracker.analyse('r1', ['repo']);
    expect(tracker.stateFor('r1')).toEqual({ phase: 'active', label: 'Queued' });
  });

  it('toasts failed runs', async () => {
    await tracker.analyse('r1', ['repo']);
    polls.get('r1-repo')?.next(aRun({ id: 'r1-repo', status: 'failed', error: 'boom' }));
    expect(toastError).toHaveBeenCalledWith('An analysis run failed', 'boom');
    expect(tracker.stateFor('r1').phase).toBe('failed');
  });

  it('marks a run failed when polling errors', async () => {
    await tracker.analyse('r1', ['repo']);
    polls.get('r1-repo')?.error(new Error('network'));
    expect(tracker.stateFor('r1').phase).toBe('failed');
  });

  it('tracks every run returned by run-all', async () => {
    expect(await tracker.analyseAll()).toBe(2);
    expect(tracker.isActive('r1')).toBe(true);
    expect(tracker.isActive('r2')).toBe(true);
  });

  it('does not poll runs that are already terminal', async () => {
    api.runAll.mockReturnValue(of([aRun({ id: 'done', status: 'succeeded' })]));
    await tracker.analyseAll();
    expect(polls.has('done')).toBe(false);
    expect(tracker.settledCount()).toBe(1);
  });
});
