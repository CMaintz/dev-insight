import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { SessionStore } from '../app/core/auth/session.store';
import { Profile } from '../app/core/models/api.models';
import { aProfile } from './fixtures';

type Matcher = Parameters<HttpTestingController['match']>[0];
type FlushOptions = Parameters<TestRequest['flush']>[1];
type PlannedResponse = [Matcher, Parameters<TestRequest['flush']>[0], FlushOptions?];

export const NOT_FOUND = { status: 404, statusText: 'Not Found' };

export interface PageHarness<T> {
  fixture: ComponentFixture<T>;
  http: HttpTestingController;
  element: HTMLElement;
  settle(): Promise<void>;
  click(label: string): Promise<void>;
  fieldValue(selector: string): string | undefined;
  respond(
    match: Matcher,
    body: Parameters<TestRequest['flush']>[0],
    options?: FlushOptions,
  ): Promise<void>;
}

function expectSingle(http: HttpTestingController, match: Matcher): TestRequest {
  const requests = http.match(match);
  if (requests.length !== 1) {
    throw new Error(`Expected one matching request, found ${requests.length}`);
  }
  return requests[0];
}

/** Not whenStable(): pending HttpTestingController requests would keep the app unstable. */
async function settle(): Promise<void> {
  for (let i = 0; i < 3; i++) {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve, 0));
  }
  TestBed.tick();
}

function configurePageTestBed(component: Type<unknown>, profile: Profile | null): void {
  TestBed.configureTestingModule({
    imports: [component],
    providers: [
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideNoopAnimations(),
    ],
  });
  if (profile) {
    TestBed.inject(SessionStore).setProfile(profile);
  }
}

function applyInputs<T>(fixture: ComponentFixture<T>, inputs: Record<string, unknown>): void {
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
}

type PageActions = Pick<PageHarness<unknown>, 'settle' | 'respond' | 'click' | 'fieldValue'>;

function pageActions(element: HTMLElement, http: HttpTestingController): PageActions {
  return {
    settle,
    respond: async (match, body, options) => {
      expectSingle(http, match).flush(body, options);
      await settle();
    },
    click: async (label) => {
      button(element, label).click();
      await settle();
    },
    fieldValue: (selector) =>
      element.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector)?.value,
  };
}

/**
 * A routed page with HttpTestingController and (optionally) a signed-in session. Real ngx-charts
 * render under jsdom (600×400 fallback viewport). Inputs apply before the first change detection.
 */
export function createPage<T>(
  component: Type<T>,
  inputs: Record<string, unknown> = {},
  profile: Profile | null = aProfile(),
): PageHarness<T> {
  configurePageTestBed(component, profile);
  const fixture = TestBed.createComponent(component);
  applyInputs(fixture, inputs);
  const http = TestBed.inject(HttpTestingController);
  const element = fixture.nativeElement as HTMLElement;
  return { fixture, http, element, ...pageActions(element, http) };
}

/** Creates the page, lets it issue its first requests, and answers them in order. */
export async function openPage<T>(
  component: Type<T>,
  responses: PlannedResponse[],
  inputs: Record<string, unknown> = {},
): Promise<PageHarness<T>> {
  const page = createPage(component, inputs);
  await page.settle();
  for (const [match, body, options] of responses) {
    expectSingle(page.http, match).flush(body, options);
  }
  await page.settle();
  return page;
}

export function text(element: HTMLElement): string {
  return (element.textContent ?? '').replace(/\s+/g, ' ');
}

export function button(element: HTMLElement, label: string): HTMLButtonElement {
  const match = [...element.querySelectorAll('button')].find((b) => text(b).trim().includes(label));
  if (!match) {
    throw new Error(`No button labelled "${label}"`);
  }
  return match;
}

export function typeInto(element: HTMLElement, selector: string, value: string): void {
  const input = requireElement<HTMLInputElement | HTMLTextAreaElement>(element, selector);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('blur'));
}

export function requireElement<T extends Element>(root: HTMLElement, selector: string): T {
  const found = root.querySelector<T>(selector);
  if (!found) {
    throw new Error(`No element matching ${selector}`);
  }
  return found;
}
