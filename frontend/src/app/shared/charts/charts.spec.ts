import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { aDashboard } from '../../../testing/fixtures';
import { ChartFrame } from './chart-frame';
import { ActivityChart, CommitSizeChart, LanguagesChart, ScoreEvolutionChart } from './charts';
import { TimeSeriesChart } from './time-series-chart';

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

  it('TimeSeriesChart draws its legend outside the measured plot', async () => {
    const fixture = TestBed.createComponent(TimeSeriesChart);
    const point = { name: new Date('2026-09-07T00:00:00Z'), value: 50 };
    fixture.componentRef.setInput('series', [
      { name: 'Overall', series: [point] },
      { name: 'Activity', series: [point] },
    ]);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    const items = [...element.querySelectorAll(':scope > .legend li')];
    expect(items.map((li) => li.textContent?.trim())).toEqual(['Overall', 'Activity']);
    expect(element.querySelector('.plot .chart-legend, .plot .legend')).toBeNull();
  });

  it('TimeSeriesChart leaves out the legend for a single series', async () => {
    const fixture = TestBed.createComponent(TimeSeriesChart);
    fixture.componentRef.setInput('series', [{ name: 'Commits', series: [] }]);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('.legend')).toBeNull();
  });
});
