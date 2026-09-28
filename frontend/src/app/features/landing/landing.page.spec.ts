import { TestBed } from '@angular/core/testing';
import { BROWSER_LOCATION } from '../../core/auth/browser-location';
import { SessionStore } from '../../core/auth/session.store';
import { aProfile } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { NotFoundPage } from '../not-found/not-found.page';
import { LandingPage } from './landing.page';

describe('LandingPage', () => {
  it('pitches the product and starts GitHub sign-in', async () => {
    const assign = vi.fn();
    TestBed.overrideProvider(BROWSER_LOCATION, { useValue: { assign } });
    const { element, settle } = createPage(LandingPage, {}, null);
    TestBed.inject(SessionStore).markSignedOut();
    await settle();

    expect(text(element)).toContain('analyses your GitHub codebases');
    button(element, 'Sign in with GitHub').click();
    expect(assign).toHaveBeenCalledWith('/api/auth/github/login?returnUrl=%2Fdashboard');
  });

  it('links signed-in users to their dashboard', async () => {
    const { element, settle } = createPage(LandingPage, {}, aProfile());
    await settle();
    expect(element.querySelector('a[href="/dashboard"]')?.textContent).toContain('dashboard');
  });
});

describe('NotFoundPage', () => {
  it('links back home', async () => {
    const { element, settle } = createPage(NotFoundPage, {}, null);
    await settle();
    expect(text(element)).toContain('This page does not exist');
  });
});
