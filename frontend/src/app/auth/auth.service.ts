import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { LoginResponse, User } from './auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  private readonly apiOreTracking = '';
  private readonly apiPmAssistant = 'http://localhost:5000';

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiOreTracking}/api/auth/login`, { username, password })
      .pipe(
        tap((res) => {
          localStorage.setItem('authToken', res.token);
          this.currentUserSubject.next({ username, roles: ['user'] });
        }),
      );
  }

  logout(): void {
    localStorage.removeItem('authToken');
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
