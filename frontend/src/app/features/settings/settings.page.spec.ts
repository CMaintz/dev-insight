import { TestBed } from '@angular/core/testing';
import { SessionStore } from '../../core/auth/session.store';
import { aProfile } from '../../../testing/fixtures';
import { button, createPage, text } from '../../../testing/page-harness';
import { SettingsPage, toProfileUpdate } from './settings.page';

function type(element: HTMLElement, selector: string, value: string) {
  const input = element.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
  if (!input) throw new Error(`missing ${selector}`);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('blur'));
}

describe('SettingsPage', () => {
  it('prefills the form and links to the portfolio preview', async () => {
    const { element, settle } = createPage(SettingsPage, {}, aProfile({ bio: 'Hello' }));
    await settle();
    expect(element.querySelector<HTMLTextAreaElement>('#bio')?.value).toBe('Hello');
    expect(text(element)).toContain('Not published yet');
    expect(element.querySelector('a[href="/u/octocat"]')).not.toBeNull();
  });

  it('rejects non-LinkedIn URLs', async () => {
    const { element, http, settle } = createPage(SettingsPage);
    await settle();
    type(element, '#linkedin', 'https://example.com/me');
    button(element, 'Save changes').click();
    await settle();
    expect(text(element)).toContain('Enter an https:// link on linkedin.com.');
    http.expectNone('/api/me/profile');
  });

  it('saves the profile and updates the session', async () => {
    const { element, http, settle } = createPage(SettingsPage);
    await settle();
    type(element, '#bio', ' Builder of things ');
    type(element, '#linkedin', 'https://www.linkedin.com/in/octocat');
    element.querySelector<HTMLInputElement>('input[type="checkbox"]')?.click();
    button(element, 'Save changes').click();
    await settle();

    const put = http.expectOne({ method: 'PUT', url: '/api/me/profile' });
    expect(put.request.body).toEqual({
      bio: 'Builder of things',
      linkedInUrl: 'https://www.linkedin.com/in/octocat',
      isPortfolioPublic: true,
    });
    put.flush(aProfile({ isPortfolioPublic: true }));
    await settle();
    expect(TestBed.inject(SessionStore).profile()?.isPortfolioPublic).toBe(true);
    expect(text(element)).toContain('Published');
  });
});

describe('toProfileUpdate', () => {
  it('maps blanks to null', () => {
    expect(toProfileUpdate({ bio: ' ', linkedInUrl: '', isPortfolioPublic: false })).toEqual({
      bio: null,
      linkedInUrl: null,
      isPortfolioPublic: false,
    });
  });
});
