import { HttpTestingController } from '@angular/common/http/testing';
import { aFeedback, aRepo, aRun, anAnalysis } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { TestBed } from '@angular/core/testing';
import { FeedbackList } from './feedback-list';
import { RepositoryDetailPage } from './repository-detail.page';

function flushRepo(http: HttpTestingController) {
  http.expectOne('/api/repos/r1').flush(aRepo());
}

describe('RepositoryDetailPage', () => {
  it('explains the score with metrics, files and feedback', async () => {
    const { http, element, settle } = createPage(RepositoryDetailPage, { id: 'r1' });
    await settle();
    flushRepo(http);
    http.expectOne((r) => r.url === '/api/analysis/r1').flush(anAnalysis());
    http.expectOne((r) => r.url === '/api/analysis/r1/history').flush([]);
    await settle();

    const content = text(element);
    expect(content).toContain('octocat/alpha');
    expect(element.querySelector('[aria-label="Overall: 72 out of 100, Fair"]')).not.toBeNull();
    expect(content).toContain('Why this score?');
    expect(content).toContain('Total commits');
    expect(content).toContain('50%');
    expect(content).toContain('informational');
    expect(content).toContain('src/big.ts');
    expect(content).toContain('Improvements (1)');
    expect(content).toContain('Strengths (1)');
    expect(content).toContain('abcdef1');
  });

  it('offers to analyse when there is no analysis yet (404)', async () => {
    const { http, element, settle } = createPage(RepositoryDetailPage, { id: 'r1' });
    await settle();
    flushRepo(http);
    http
      .expectOne((r) => r.url === '/api/analysis/r1')
      .flush(null, { status: 404, statusText: 'Not Found' });
    http.expectOne((r) => r.url === '/api/analysis/r1/history').flush([]);
    await settle();

    expect(text(element)).toContain('Not analysed yet');
    button(element, 'Analyse now').click();
    await settle();
    http
      .expectOne((r) => r.url === '/api/analysis/run/r1' && r.params.get('scope') === 'repo')
      .flush(aRun());
    http
      .expectOne((r) => r.url === '/api/analysis/run/r1' && r.params.get('scope') === 'user')
      .flush(aRun({ id: 'run2' }));
    await settle();
    expect(text(element)).toContain('results appear here when the run finishes');
  });

  it('reports a missing repository', async () => {
    const { http, element, settle } = createPage(RepositoryDetailPage, { id: 'r1' });
    await settle();
    http.expectOne('/api/repos/r1').flush(null, { status: 404, statusText: 'Not Found' });
    http
      .expectOne((r) => r.url === '/api/analysis/r1')
      .flush(null, { status: 404, statusText: 'Not Found' });
    http.expectOne((r) => r.url === '/api/analysis/r1/history').flush([]);
    await settle();
    expect(text(element)).toContain('Repository not found');
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
