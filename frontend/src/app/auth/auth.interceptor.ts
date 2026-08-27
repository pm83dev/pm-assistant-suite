import { HttpInterceptorFn, HttpResponse, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Router, UrlTree } from '@angular/router';
import { catchError, throwError, finalize, Observable, map } from 'rxjs';

export const AuthInterceptor: HttpInterceptorFn = (req, next): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const router = inject(Router);
  let tokenRefreshed = false;

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
      if (error.status === 401 && !tokenRefreshed && authService.getToken()) {
        tokenRefreshed = true;
        
        // Riprova con il refresh del token
        return authService.refreshToken().pipe(
          map(() => {
            // Se il refresh ha successo, ricrea la richiesta con il nuovo token
            const newToken = authService.getToken();
            const newReq = req.clone({
              setHeaders: { Authorization: `Bearer ${newToken}` }
            });
            return next(newReq);
          }),
          catchError((refreshError: any): Observable<UrlTree> => {
            // Se il refresh fallisce, logout e redirect a login
            authService.logout();
            return new Observable<UrlTree>(observer => {
              observer.next(router.parseUrl('/login'));
              observer.complete();
            });
          }),
          finalize(() => {
            tokenRefreshed = false;
          })
        );
      }
      
      // Per altri errori, restituisci l'errore originale
      return throwError(() => error);
    })
  ) as Observable<HttpEvent<unknown>>;
};
