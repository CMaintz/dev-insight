import { TestBed } from '@angular/core/testing';
import { answerRunStarts } from '../../../testing/analysis-requests';
import { aFeedback, aRepo, aRun, anAnalysis } from '../../../testing/fixtures';
import { NOT_FOUND, openPage, text } from '../../../testing/page-harness';
import { Analysis, Repository } from '../../core/models/api.models';
import { FeedbackList } from './feedback-list';
import { RepositoryDetailPage } from './repository-detail.page';

const MISSING = null;

function openDetail(repo: Repository | null, analysis: Analysis | null) {
  return openPage(
    RepositoryDetailPage,
    [
      ['/api/repos/r1', repo, repo ? undefined : NOT_FOUND],
      [(r) => r.url === '/api/analysis/r1', analysis, analysis ? undefined : NOT_FOUND],
      [(r) => r.url === '/api/analysis/r1/history', []],
    ],
    { id: 'r1' },
  );
}

describe('RepositoryDetailPage', () => {
  it('shows the repository header and the score rings', async () => {
    const { element } = await openDetail(aRepo(), anAnalysis());
    expect(text(element)).toContain('octocat/alpha');
    expect(text(element)).toContain('abcdef1');
    expect(element.querySelector('[aria-label="Overall: 72 out of 100, Fair"]')).not.toBeNull();
  });

  it('explains the score with scored and informational metrics', async () => {
    const content = text((await openDetail(aRepo(), anAnalysis())).element);
    expect(content).toContain('Why this score?');
    expect(content).toContain('Total commits');
    expect(content).toContain('50%');
    expect(content).toContain('informational');
  });

  it('lists the largest files and the feedback', async () => {
    const content = text((await openDetail(aRepo(), anAnalysis())).element);
    expect(content).toContain('src/big.ts');
    expect(content).toContain('Improvements (1)');
    expect(content).toContain('Strengths (1)');
  });

  it('offers to analyse when there is no analysis yet (404)', async () => {
    const page = await openDetail(aRepo(), MISSING);
    expect(text(page.element)).toContain('Not analysed yet');
    await page.click('Analyse now');
    answerRunStarts(page.http, 'r1', (scope) => aRun({ id: `run-${scope}` }));
    await page.settle();
    expect(text(page.element)).toContain('results appear here when the run finishes');
  });

  it('reports a missing repository without offering an analysis', async () => {
    const content = text((await openDetail(MISSING, MISSING)).element);
    expect(content).toContain('Repository not found');
    expect(content).not.toContain('Not analysed yet');
    expect(content).not.toContain('Analyse now');
  });
});

describe('FeedbackList', () => {
  it('lists improvements most severe first and strengths separately', async () => {
    const fixture = TestBed.createComponent(FeedbackList);
    fixture.componentRef.setInput('feedback', [
      aFeedback({ id: 'l', severity: 'low', title: 'Low one' }),
      aFeedback({ id: 's', isStrength: true, title: 'Strong one' }),
      aFeedback({ id: 'h', severity: 'high', title: 'High one' }),
    ]);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const titles = (selector: string) =>
      [...element.querySelectorAll(`${selector} h4`)].map((h) => h.textContent?.trim());
    expect(titles('section[aria-labelledby="fb-improve"]')).toEqual(['High one', 'Low one']);
    expect(titles('section[aria-labelledby="fb-strength"]')).toEqual(['Strong one']);
  });
});
