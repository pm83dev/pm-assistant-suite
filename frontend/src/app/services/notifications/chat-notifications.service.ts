import { Injectable } from '@angular/core';
import { Subject, BehaviorSubject } from 'rxjs';

export interface ChatNotification {
  id: string;
  type: 'message' | 'error' | 'tool-result' | 'tool-error';
  title: string;
  message: string;
  timestamp: Date;
  data?: any;
}

@Injectable({
  providedIn: 'root'
})
export class ChatNotificationsService {
  private notificationsSubject = new BehaviorSubject<ChatNotification[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  private notificationIdCounter = 0;

  constructor() { }

  addNotification(notification: Omit<ChatNotification, 'id' | 'timestamp'>): void {
    const newNotification: ChatNotification = {
      id: `notification-${++this.notificationIdCounter}`,
      timestamp: new Date(),
      ...notification
    };

    const currentNotifications = this.notificationsSubject.value;
    this.notificationsSubject.next([...currentNotifications, newNotification]);

    // Auto-remove after 5 seconds for non-error notifications
    if (notification.type !== 'error') {
      setTimeout(() => {
        this.removeNotification(newNotification.id);
      }, 5000);
    }
  }

  removeNotification(notificationId: string): void {
    const currentNotifications = this.notificationsSubject.value;
    this.notificationsSubject.next(
      currentNotifications.filter(n => n.id !== notificationId)
    );
  }

  clearAllNotifications(): void {
    this.notificationsSubject.next([]);
  }

  // Convenience methods
  addMessageNotification(message: string, title = 'Nuovo messaggio'): void {
    this.addNotification({
      type: 'message',
      title,
      message
    });
  }

  addErrorNotification(error: string, title = 'Errore'): void {
    this.addNotification({
      type: 'error',
      title,
      message: error
    });
  }

  addToolResultNotification(toolName: string, result: any): void {
    this.addNotification({
      type: 'tool-result',
      title: `Risultato ${toolName}`,
      message: this.formatToolResult(result),
      data: result
    });
  }

  addToolErrorNotification(toolName: string, error: string): void {
    this.addNotification({
      type: 'tool-error',
      title: `Errore ${toolName}`,
      message: error
    });
  }

  private formatToolResult(result: any): string {
    try {
      return JSON.stringify(result, null, 2);
    } catch {
      return String(result);
    }
  }
}