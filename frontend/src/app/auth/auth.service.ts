import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap, catchError, throwError } from 'rxjs';
import { LoginResponse, User } from './auth.model';
import { environment } from '../../environments/index';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  private readonly apiOreTracking = environment.apiOreTracking;
  private readonly apiPmAssistant = environment.apiPmAssistant;

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiOreTracking}/auth/login`, { username, password })
      .pipe(
        tap((res) => {
          localStorage.setItem('authToken', res.token);
          this.currentUserSubject.next({ username, roles: ['user'] });
        }),
        catchError((error) => {
          // Rimuovi il token se la login fallisce
          localStorage.removeItem('authToken');
          this.currentUserSubject.next(null);
          throw error;
        })
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

  refreshToken(): Observable<LoginResponse> {
    const currentToken = this.getToken();
    if (!currentToken) {
      return throwError(() => new Error('No token to refresh'));
    }

    return this.http.post<LoginResponse>(`${this.apiOreTracking}/auth/refresh`, {}, {
      headers: { Authorization: `Bearer ${currentToken}` }
    }).pipe(
      tap((res) => {
        localStorage.setItem('authToken', res.token);
      })
    );
  }
}
