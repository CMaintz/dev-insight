import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Dashboard, ScopeParam } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  resource(scope: () => ScopeParam) {
    return httpResource<Dashboard>(() => ({ url: '/api/dashboard', params: { scope: scope() } }));
  }
}
