import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { aDashboard } from '../../../testing/fixtures';
import { ChartFrame } from './chart-frame';
import { ActivityChart, CommitSizeChart, LanguagesChart, ScoreEvolutionChart } from './charts';

describe('chart widgets', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideNoopAnimations()] });
  });

  it('ChartFrame renders the caption, summary and a data-table twin', async () => {
    const fixture = TestBed.createComponent(ChartFrame);
    fixture.componentRef.setInput('heading', 'Commits');
    fixture.componentRef.setInput('summary', '7 commits');
    fixture.componentRef.setInput('table', { columns: ['Week', 'Commits'], rows: [['Sep 7', 7]] });
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('figcaption h3')?.textContent).toContain('Commits');
    expect(element.querySelector('.chart__summary')?.textContent).toContain('7 commits');
    expect(element.querySelectorAll('tbody td')).toHaveLength(2);
    expect(element.querySelector('.chart__plot')?.getAttribute('aria-hidden')).toBe('true');
  });

  it('ChartFrame shows the empty text instead of a plot', async () => {
    const fixture = TestBed.createComponent(ChartFrame);
    fixture.componentRef.setInput('heading', 'Commits');
    fixture.componentRef.setInput('summary', 'none');
    fixture.componentRef.setInput('empty', true);
    fixture.componentRef.setInput('emptyText', 'Nothing yet');
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.chart__plot')).toBeNull();
    expect(element.textContent).toContain('Nothing yet');
  });

  it('chart widgets render their summaries in empty state', async () => {
    const activity = TestBed.createComponent(ActivityChart);
    activity.componentRef.setInput('weeks', []);
    const scores = TestBed.createComponent(ScoreEvolutionChart);
    scores.componentRef.setInput('points', []);
    const languages = TestBed.createComponent(LanguagesChart);
    languages.componentRef.setInput('languages', []);
    const sizes = TestBed.createComponent(CommitSizeChart);
    sizes.componentRef.setInput('quality', { ...aDashboard().commitQuality, commits: 0 });
    await Promise.all([activity, scores, languages, sizes].map((f) => f.whenStable()));

    expect(activity.nativeElement.textContent).toContain('No commit activity recorded yet.');
    expect(scores.nativeElement.textContent).toContain('No score history yet');
    expect(languages.nativeElement.textContent).toContain('No language data yet.');
    expect(sizes.nativeElement.textContent).toContain('No commits analysed yet.');
  });
});
