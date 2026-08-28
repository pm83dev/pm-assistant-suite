import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface User {
  id: string;
  email: string;
  name: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private tokenSubject = new BehaviorSubject<string | null>(null);
  public token$ = this.tokenSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {
    this.loadAuthState();
  }

  login(email: string, password: string): Observable<AuthResponse> {
    // TODO: Implementare la chiamata API di login
    // Per ora restituiamo un mock response
    
    const mockUser: User = {
      id: '1',
      email,
      name: 'Utente Demo',
      role: 'user'
    };

    const mockResponse: AuthResponse = {
      token: 'mock-jwt-token-' + Date.now(),
      user: mockUser
    };

    this.setAuthState(mockResponse.token, mockUser);
    return new Observable(observer => {
      observer.next(mockResponse);
      observer.complete();
    });
  }

  logout(): void {
    this.clearAuthState();
  }

  getToken(): string | null {
    return this.tokenSubject.value;
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  private setAuthState(token: string, user: User): void {
    this.tokenSubject.next(token);
    this.currentUserSubject.next(user);
    this.isAuthenticatedSubject.next(true);
    
    // Salva lo stato in localStorage
    localStorage.setItem('authToken', token);
    localStorage.setItem('currentUser', JSON.stringify(user));
  }

  private clearAuthState(): void {
    this.tokenSubject.next(null);
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    
    // Rimuovi dallo stato locale
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
  }

  private loadAuthState(): void {
    const token = localStorage.getItem('authToken');
    const userStr = localStorage.getItem('currentUser');

    if (token && userStr) {
      try {
        const user = JSON.parse(userStr);
        this.setAuthState(token, user);
      } catch (error) {
        console.error('Error loading auth state:', error);
        this.clearAuthState();
      }
    }
  }

  // Metodo per verificare la validità del token
  private isTokenValid(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiration = payload.exp * 1000;
      return Date.now() < expiration;
    } catch {
      return false;
    }
  }

  // Metodo per refresh del token (se necessario)
  refreshToken(): Observable<string> {
    // TODO: Implementare il refresh del token
    // Per ora restituiamo un nuovo token mock
    const newToken = 'mock-refresh-token-' + Date.now();
    return new Observable(observer => {
      observer.next(newToken);
      observer.complete();
    });
  }
}