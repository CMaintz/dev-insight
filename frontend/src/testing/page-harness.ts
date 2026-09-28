import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { SessionStore } from '../app/core/auth/session.store';
import { Profile } from '../app/core/models/api.models';
import { aProfile } from './fixtures';

export interface PageHarness<T> {
  fixture: ComponentFixture<T>;
  http: HttpTestingController;
  element: HTMLElement;
  settle(): Promise<void>;
}

/**
 * Creates a routed page with HttpTestingController and (optionally) a signed-in session.
 * Real ngx-charts render fine under jsdom (they fall back to a 600×400 viewport).
 * Inputs are applied before the first change detection.
 */
export function createPage<T>(
  component: Type<T>,
  inputs: Record<string, unknown> = {},
  profile: Profile | null = aProfile(),
): PageHarness<T> {
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
  const fixture = TestBed.createComponent(component);
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  const http = TestBed.inject(HttpTestingController);
  // Not whenStable(): pending HttpTestingController requests would keep the app unstable.
  const settle = async () => {
    for (let i = 0; i < 3; i++) {
      TestBed.tick();
      await new Promise((resolve) => setTimeout(resolve, 0));
    }
    TestBed.tick();
  };
  return { fixture, http, element: fixture.nativeElement as HTMLElement, settle };
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
