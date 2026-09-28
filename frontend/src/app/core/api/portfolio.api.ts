import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Portfolio } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PortfolioApi {
  /** Public portfolio by login or user id. A 404 means "not found or not published". */
  resource(handle: () => string | undefined) {
    return httpResource<Portfolio>(() => {
      const value = handle();
      return value ? `/api/portfolio/${encodeURIComponent(value)}` : undefined;
    });
  }
}
