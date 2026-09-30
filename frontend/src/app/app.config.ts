import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withViewTransitions,
} from '@angular/router';
import { routes } from './app.routes';
import { AppConfig, loadAppConfig } from './core/config/app-config';
import { apiBaseUrlInterceptor, bearerTokenInterceptor } from './core/http/api.interceptors';
import { errorInterceptor } from './core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Runtime config (API base URL) must be known before the first HTTP request.
    provideAppInitializer(() => {
      const config = inject(AppConfig);
      return loadAppConfig().then((data) => config.applyLoaded(data));
    }),
    // Order matters: errors see the relative URL; the bearer token is attached only to our
    // relative API paths; the base URL is applied last.
    provideHttpClient(
      withInterceptors([errorInterceptor, bearerTokenInterceptor, apiBaseUrlInterceptor]),
    ),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
      withViewTransitions(),
    ),
    // ngx-charts uses @angular/animations; the async provider keeps the engine out of main.js.
    provideAnimationsAsync(),
  ],
};
