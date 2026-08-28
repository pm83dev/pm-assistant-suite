import { Injectable } from '@angular/core';
import { ChatNotificationsService, ChatNotification } from '../notifications/chat-notifications.service';

export interface ErrorContext {
  endpoint?: string;
  method?: string;
  status?: number;
  message?: string;
  timestamp: Date;
  userAction?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  constructor(
    private notificationsService: ChatNotificationsService
  ) {}

  handleError(error: any, context?: ErrorContext): void {
    console.error('Error occurred:', error);

    let notification: ChatNotification;

    if (this.isHttpError(error)) {
      notification = this.handleHttpError(error, context);
    } else if (this.isNetworkError(error)) {
      notification = this.handleNetworkError(context);
    } else if (this.isServerError(error)) {
      notification = this.handleServerError(error, context);
    } else {
      notification = this.handleGenericError(error, context);
    }

    this.notificationsService.addNotification(notification);
  }

  private isHttpError(error: any): boolean {
    return error.status !== undefined && error.status !== null;
  }

  private isNetworkError(error: any): boolean {
    return error.name === 'NetworkError' || 
           error.message?.includes('Network') ||
           !navigator.onLine;
  }

  private isServerError(error: any): boolean {
    return this.isHttpError(error) && error.status >= 500;
  }

  private handleHttpError(error: any, context?: ErrorContext): ChatNotification {
    const status = error.status || 0;
    const message = this.getHttpErrorMessage(status, error);
    
    return {
      id: `error-http-${Date.now()}`,
      type: 'error',
      title: `Errore ${status}`,
      message,
      timestamp: new Date(),
      data: {
        status,
        statusText: error.statusText,
        url: error.url,
        context
      }
    };
  }

  private handleNetworkError(context?: ErrorContext): ChatNotification {
    return {
      id: `error-network-${Date.now()}`,
      type: 'error',
      title: 'Errore di Connessione',
      message: 'Impossibile connettersi al server. Controlla la tua connessione internet.',
      timestamp: new Date(),
      data: { context }
    };
  }

  private handleServerError(error: any, context?: ErrorContext): ChatNotification {
    return {
      id: `error-server-${Date.now()}`,
      type: 'error',
      title: 'Errore del Server',
      message: 'Il server ha riportato un errore. Riprova più tardi.',
      timestamp: new Date(),
      data: { 
        error: error.message,
        context,
        suggestion: 'Contatta l\'amministratore del sistema se il problema persiste'
      }
    };
  }

  private handleGenericError(error: any, context?: ErrorContext): ChatNotification {
    return {
      id: `error-generic-${Date.now()}`,
      type: 'error',
      title: 'Errore Generico',
      message: error.message || 'Si è verificato un errore imprevisto.',
      timestamp: new Date(),
      data: { error, context }
    };
  }

  private getHttpErrorMessage(status: number, error: any): string {
    switch (status) {
      case 400:
        return 'Richiesta non valida. Controlla i dati inseriti.';
      case 401:
        return 'Non autorizzato. Effettua il login.';
      case 403:
        return 'Accesso negato. Non hai i permessi necessari.';
      case 404:
        return 'Risorsa non trovata.';
      case 409:
        return 'Conflitto con altre risorse.';
      case 422:
        return 'Dati non validi. Correggi i campi evidenziati.';
      case 429:
        return 'Troppe richieste. Prova più tardi.';
      case 500:
        return 'Errore interno del server.';
      case 502:
        return 'Server non raggiungibile.';
      case 503:
        return 'Servizio temporaneamente non disponibile.';
      default:
        return error.message || `Errore ${status}`;
    }
  }

  showSuccess(message: string, title = 'Successo'): void {
    this.notificationsService.addNotification({
      type: 'message',
      title,
      message
    });
  }

  showWarning(message: string, title = 'Attenzione'): void {
    this.notificationsService.addNotification({
      type: 'message',
      title,
      message
    });
  }

  showInfo(message: string, title = 'Informazione'): void {
    this.notificationsService.addNotification({
      type: 'message',
      title,
      message
    });
  }
}