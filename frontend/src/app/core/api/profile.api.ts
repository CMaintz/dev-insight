import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Profile, ProfileUpdate } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProfileApi {
  private readonly http = inject(HttpClient);

  me(): Observable<Profile> {
    return this.http.get<Profile>('/api/me');
  }

  update(body: ProfileUpdate): Observable<Profile> {
    return this.http.put<Profile>('/api/me/profile', body);
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', null);
  }
}
