import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { aProfile } from '../testing/fixtures';
import { App } from './app';

describe('App', () => {
  it('renders the shell and shows navigation once the session resolves', async () => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/me').flush(aProfile());
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('main#main')).not.toBeNull();
    expect(element.querySelector('nav[aria-label="Main"]')?.textContent).toContain('Dashboard');
    expect(element.textContent).toContain('Sign out');
    http.verify();
  });

  it('offers sign-in to anonymous visitors', async () => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController)
      .expectOne('/api/me')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('nav')).toBeNull();
    expect(element.textContent).toContain('Sign in');
  });
});
