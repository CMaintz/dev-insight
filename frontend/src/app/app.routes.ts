import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'DevInsight — see how your code evolves',
    loadComponent: () => import('./features/landing/landing.page').then((m) => m.LandingPage),
  },
  {
    path: 'auth/callback',
    title: 'Signing in · DevInsight',
    loadComponent: () =>
      import('./features/auth-callback/auth-callback.page').then((m) => m.AuthCallbackPage),
  },
  {
    path: 'u/:handle',
    title: 'Portfolio · DevInsight',
    loadComponent: () => import('./features/portfolio/portfolio.page').then((m) => m.PortfolioPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        title: 'Dashboard · DevInsight',
        loadComponent: () =>
          import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage),
      },
      {
        path: 'repositories',
        title: 'Repositories · DevInsight',
        loadComponent: () =>
          import('./features/repositories/repositories.page').then((m) => m.RepositoriesPage),
      },
      {
        path: 'repositories/:id',
        title: 'Repository · DevInsight',
        loadComponent: () =>
          import('./features/repository-detail/repository-detail.page').then(
            (m) => m.RepositoryDetailPage,
          ),
      },
      {
        path: 'projects',
        title: 'Projects · DevInsight',
        loadComponent: () =>
          import('./features/projects/projects.page').then((m) => m.ProjectsPage),
      },
      {
        path: 'settings',
        title: 'Settings · DevInsight',
        loadComponent: () =>
          import('./features/settings/settings.page').then((m) => m.SettingsPage),
      },
    ],
  },
  {
    path: '**',
    title: 'Not found · DevInsight',
    loadComponent: () => import('./features/not-found/not-found.page').then((m) => m.NotFoundPage),
  },
];
