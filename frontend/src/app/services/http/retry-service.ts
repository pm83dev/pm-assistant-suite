import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ChatLoggerService, LogLevel } from '../logging/chat-logger.service';

export interface RetryConfig {
  maxRetries: number;
  retryDelay: number;
  retryableStatusCodes: number[];
  exponentialBackoff: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class RetryService {
  private defaultConfig: RetryConfig = {
    maxRetries: 3,
    retryDelay: 1000,
    retryableStatusCodes: [408, 429, 500, 502, 503, 504],
    exponentialBackoff: true
  };

  constructor(
    private logger: ChatLoggerService
  ) {}

  async retryOperation<T>(
    operation: () => Promise<T>,
    config?: Partial<RetryConfig>
  ): Promise<T> {
    const retryConfig = { ...this.defaultConfig, ...config };
    let lastError: Error;

    for (let attempt = 0; attempt <= retryConfig.maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        lastError = error as Error;

        if (attempt === retryConfig.maxRetries || !this.shouldRetry(lastError, retryConfig)) {
          this.logger.error('Max retries reached', 'retry', {
            error: lastError.message,
            config: retryConfig
          });
          throw lastError;
        }

        const delay = this.calculateDelay(attempt, retryConfig);
        this.logger.warn(`Retry attempt ${attempt + 1}/${retryConfig.maxRetries + 1}`, 'retry', {
          error: lastError.message,
          delay,
          config: retryConfig
        });

        await this.delay(delay);
      }
    }

    throw lastError!;
  }

  private shouldRetry(error: Error, config: RetryConfig): boolean {
    if (error instanceof HttpErrorResponse) {
      // Retry for network errors or specific HTTP status codes
      if (!navigator.onLine) {
        return true; // Network error
      }

      return config.retryableStatusCodes.includes(error.status || 0);
    }

    // Retry for other types of errors (es. timeout, etc.)
    return error.name === 'TimeoutError' || 
           error.message?.includes('timeout') ||
           error.message?.includes('network');
  }

  private calculateDelay(attempt: number, config: RetryConfig): number {
    let delay = config.retryDelay;

    if (config.exponentialBackoff) {
      delay *= Math.pow(2, attempt); // Esponenziale: 1s, 2s, 4s, 8s...
    }

    // Aggiungi jitter casuale per evitare thundering herd
    const jitter = delay * 0.1 * (Math.random() - 0.5);
    return Math.round(delay + jitter);
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  // Metodo per creare una versione retry della funzione originale
  createRetryFunction<T>(
    operation: () => Promise<T>,
    config?: Partial<RetryConfig>
  ): () => Promise<T> {
    return () => this.retryOperation(operation, config);
  }

  // Metodo per gestire errori specifici
  handleSpecificError(error: HttpErrorResponse): string {
    switch (error.status) {
      case 400:
        return 'La richiesta contiene dati non validi. Controlla i parametri.';
      case 401:
        return 'Non autorizzato. Effettua il login.';
      case 403:
        return 'Accesso negato. Non hai i permessi necessari.';
      case 404:
        return 'Risorsa non trovata.';
      case 409:
        return 'Conflitto con altre risorse. Aggiorna e riprova.';
      case 422:
        return 'Dati non validi. Correggi gli errori di validazione.';
      case 429:
        return 'Troppe richieste. Attendi prima di riprovare.';
      case 500:
        return 'Errore interno del server. Riprova più tardi.';
      case 502:
        return 'Server non raggiungibile. Riprova più tardi.';
      case 503:
        return 'Servizio temporaneamente non disponibile.';
      case 504:
        return 'Timeout della gateway. Riprova più tardi.';
      default:
        return `Errore ${error.status}: ${error.statusText}`;
    }
  }

  // Metodo per gestire timeout
  handleTimeout(timeoutMs: number): string {
    return `La richiesta è scaduta dopo ${timeoutMs}ms. Prova a ridurre i dati o aumentare il timeout.`;
  }
}