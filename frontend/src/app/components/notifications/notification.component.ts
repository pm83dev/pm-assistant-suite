import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatNotificationsService, ChatNotification } from '../../services/notifications/chat-notifications.service';

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.css']
})
export class NotificationComponent {
  private notificationsService = inject(ChatNotificationsService);
  
  notifications$ = this.notificationsService.notifications$;

  getNotificationTypeClass(type: string): string {
    switch (type) {
      case 'error':
        return 'notification-error';
      case 'tool-result':
        return 'notification-success';
      case 'tool-error':
        return 'notification-warning';
      default:
        return 'notification-info';
    }
  }

  formatTimestamp(timestamp: Date): string {
    return timestamp.toLocaleTimeString();
  }

  onRemove(notificationId: string): void {
    this.notificationsService.removeNotification(notificationId);
  }

  onClearAll(): void {
    this.notificationsService.clearAllNotifications();
  }
}