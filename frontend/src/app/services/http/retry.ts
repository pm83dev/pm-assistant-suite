import { HttpErrorResponse } from '@angular/common/http';
import { Observable, concatMap, of, retryWhen, scan, timer } from 'rxjs';

/**
 * Retry con backoff esponenziale per i fetch iniziali dei servizi di gestione.
 *
 * Reagisce solo agli errori "transitori" (timeout, network error, 408, 5xx).
 * Gli errori 4xx deterministici (401/403/404) NON vengono ritentati: sono
 * gestiti dall'intercettore di auth / dal caller.
 *
 * Il retry è applicato a livello di fetch (pipe): ogni tentativo ri-sottoscrive
 * l'intera sorgente, quindi i dati vengono riletti dall'inizio (reload corretto).
 */
export function withRetry<T>(
  source: Observable<T>,
  maxAttempts = 3,
  initialDelayMs = 1000,
): Observable<T> {
  return source.pipe(
    retryWhen((errors) =>
      errors.pipe(
        scan(
          ({ attempt, lastError }: { attempt: number; lastError: unknown }) => {
            const nextAttempt = attempt + 1;
            if (!isRetryable(lastError) || nextAttempt > maxAttempts) {
              // Non ritentabile o tentativi esauriti: ri-throw l'ultimo errore.
              throw lastError;
            }
            return { attempt: nextAttempt, lastError };
          },
          { attempt: 0, lastError: null as unknown },
        ),
        concatMap(({ attempt }: { attempt: number }) => {
          const base = initialDelayMs * Math.pow(2, attempt - 1);
          const jitter = base * 0.1 * (Math.random() - 0.5);
          const delayMs = Math.round(base + jitter);
          return timer(delayMs);
        }),
      ),
    ),
  );
}

/**
 * Decide se un errore è "transitorio" e quindi meritevole di un retry.
 * - HttpErrorResponse: retry su 408 (timeout) e 5xx; nessun retry su 4xx.
 * - Errori di rete (no status, es. offline / CORS / connection refused): retry.
 */
function isRetryable(error: unknown): boolean {
  if (error instanceof HttpErrorResponse) {
    const status = error.status;
    if (status === 0) {
      return true; // errore di rete (nessuna risposta HTTP)
    }
    if (status === 408) {
      return true; // timeout
    }
    return status >= 500 && status <= 599; // errore lato server/gateway
  }
  // Errori non HTTP (es. Error generico da rete): si tenta il retry.
  return true;
}
