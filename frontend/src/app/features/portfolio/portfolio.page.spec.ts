import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { aPortfolio } from '../../../testing/fixtures';
import { createPage, text } from '../../../testing/page-harness';
import { PortfolioPage, portfolioHighlights } from './portfolio.page';

describe('PortfolioPage', () => {
  it('renders the public showcase without a session', async () => {
    const { http, element, settle } = createPage(PortfolioPage, { handle: 'octocat' }, null);
    await settle();
    http.expectOne('/api/portfolio/octocat').flush(aPortfolio());
    await settle();

    const content = text(element);
    expect(element.querySelector('h1')?.textContent).toContain('The Octocat');
    expect(content).toContain('@octocat');
    expect(content).toContain('I write code.');
    expect(element.querySelector('a[href="https://www.linkedin.com/in/octocat"]')).not.toBeNull();
    expect(element.querySelector('a[href="https://github.com/octocat"]')).not.toBeNull();
    expect(element.querySelector('[aria-label="Overall: 81 out of 100, Strong"]')).not.toBeNull();
    expect(content).toContain('Showcase');
    expect(content).toContain('Tested');
    expect(content).not.toContain('Preview.');
    expect(TestBed.inject(Title).getTitle()).toBe('The Octocat · DevInsight portfolio');
  });

  it('switches project screenshots', async () => {
    const { http, element, settle } = createPage(PortfolioPage, { handle: 'octocat' }, null);
    await settle();
    http.expectOne('/api/portfolio/octocat').flush(aPortfolio());
    await settle();

    element.querySelector<HTMLButtonElement>('button[aria-label="Show screenshot 2"]')?.click();
    await settle();
    expect(element.querySelector('.pcard__image')?.getAttribute('src')).toBe(
      'https://example.com/2.png',
    );
  });

  it('marks an unpublished portfolio as an owner preview', async () => {
    const { http, element, settle } = createPage(PortfolioPage, { handle: 'octocat' });
    await settle();
    const portfolio = aPortfolio();
    http.expectOne('/api/portfolio/octocat').flush({
      ...portfolio,
      owner: { ...portfolio.owner, isPublic: false, avatarUrl: 'https://a/x.png' },
    });
    await settle();
    expect(text(element)).toContain('Preview.');
    expect(element.querySelector('img.hero__avatar')).not.toBeNull();
  });

  it('shows a friendly message for unknown or unpublished portfolios', async () => {
    const { http, element, settle } = createPage(PortfolioPage, { handle: 'ghost' }, null);
    await settle();
    http.expectOne('/api/portfolio/ghost').flush(null, { status: 404, statusText: 'Not Found' });
    await settle();
    expect(text(element)).toContain('Portfolio not found');
  });
});

describe('portfolioHighlights', () => {
  it('summarises the portfolio', () => {
    expect(portfolioHighlights(aPortfolio())).toEqual({
      repositories: '1',
      commits: '3',
      activeWeeks: '1',
      topLanguage: 'TypeScript',
    });
    expect(portfolioHighlights(aPortfolio({ languages: [] })).topLanguage).toBe('—');
  });
});
