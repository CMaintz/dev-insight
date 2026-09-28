import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SILENT_ERRORS } from '../http/error.interceptor';
import { TokenResponse } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  exchange(code: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(
      '/api/auth/exchange',
      { code },
      { context: new HttpContext().set(SILENT_ERRORS, true) },
    );
  }
}
