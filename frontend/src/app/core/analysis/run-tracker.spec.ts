import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
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

  it('marks a run as lost when polling errors', async () => {
    await tracker.analyse('r1', ['repo']);
    polls.get('r1-repo')?.error(new Error('network'));
    expect(tracker.stateFor('r1').phase).toBe('timedOut');
    expect(tracker.activeCount()).toBe(0);
  });

  it('times out a run when polling gives up without a final status', async () => {
    await tracker.analyse('r1', ['repo']);
    polls.get('r1-repo')?.next(aRun({ id: 'r1-repo', status: 'running' }));
    polls.get('r1-repo')?.complete();

    const state = tracker.stateFor('r1');
    expect(state.phase).toBe('timedOut');
    expect(state.error).toContain('refresh later');
    expect(tracker.activeCount()).toBe(0);
    expect(tracker.settledCount()).toBe(1);
    expect(toastError).toHaveBeenCalledWith('Lost track of an analysis run', state.error);
  });

  it('cancels the old poll when the same repository is analysed again', async () => {
    await tracker.analyse('r1', ['repo']);
    const oldPoll = polls.get('r1-repo');
    api.run.mockReturnValue(of(aRun({ id: 'second', repositoryId: 'r1' })));
    await tracker.analyse('r1', ['repo']);

    expect(oldPoll?.observed).toBe(false);
    oldPoll?.next(aRun({ id: 'r1-repo', status: 'succeeded' }));
    expect(tracker.settledCount()).toBe(0);
    expect(tracker.activeCount()).toBe(1);
  });

  it('stops every poll and forgets all runs on stopAll', async () => {
    await tracker.analyse('r1');
    tracker.stopAll();
    expect(polls.get('r1-repo')?.observed).toBe(false);
    expect(polls.get('r1-user')?.observed).toBe(false);
    expect(tracker.activeCount()).toBe(0);
    expect(tracker.stateFor('r1').phase).toBe('idle');
  });

  it('keeps tracking the scope that started when the other scope fails', async () => {
    api.run.mockImplementation((repoId: string, scope: string) =>
      scope === 'user'
        ? throwError(() => new Error('boom'))
        : of(aRun({ id: `${repoId}-${scope}`, repositoryId: repoId })),
    );
    await expect(tracker.analyse('r1')).rejects.toThrow('boom');
    expect(tracker.isActive('r1')).toBe(true);
    expect(polls.has('r1-repo')).toBe(true);
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
