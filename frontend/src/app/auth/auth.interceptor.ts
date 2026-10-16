import { HttpInterceptorFn, HttpResponse, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Router, UrlTree } from '@angular/router';
import { catchError, throwError, switchMap, from, Observable } from 'rxjs';

// Promise condivisa a livello di modulo: se più richieste ricevono 401
// contemporaneamente (token scaduto), tutte attendono lo STESSO refresh
// invece di lanciarne uno per ciascuna.
let refreshPromise: Promise<string> | null = null;

export const AuthInterceptor: HttpInterceptorFn = (req, next): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Clona la richiesta per aggiungere il token
  let request = req;
  const token = authService.getToken();

  if (token && !req.url.includes('/auth/')) {
    request = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      // Gestisci errore 401 - token scaduto o non valido
      if (error.status === 401 && authService.getToken()) {
        // Deduplica: una sola chiamata di refresh per tutti i 401 concorrenti
        if (!refreshPromise) {
          refreshPromise = authService
            .refreshToken()
            .toPromise()
            .then(() => authService.getToken() as string)
            .finally(() => {
              refreshPromise = null;
            });
        }

        return from(refreshPromise).pipe(
          // switchMap (non map): appiattisce l'Observable della richiesta
          // ritentata, altrimenti la richiesta originale non riceve mai
          // né valore né errore e il caricamento resta attivo in eterno.
          switchMap((newToken) =>
            next(
              req.clone({
                setHeaders: { Authorization: `Bearer ${newToken}` },
              })
            )
          ),
          catchError((refreshError: unknown) => {
            // Se il refresh fallisce (token scaduto senza refresh token),
            // logout e redirect a login.
            authService.logout();
            if (router.url !== '/login') {
              router.navigate(['/login']);
            }
            return throwError(() => refreshError);
          })
        );
      }

      // Per altri errori, restituisci l'errore originale
      return throwError(() => error);
    })
  ) as Observable<HttpEvent<unknown>>;
};
