import { Injectable } from '@angular/core';
import { Subject, BehaviorSubject } from 'rxjs';
import { ChatMessage, ToolCall } from '../../models/models';

export interface WebSocketMessage {
  type: 'message' | 'tool-result' | 'tool-call' | 'error' | 'connection-status';
  data: any;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ChatWebSocketService {
  private websocket: WebSocket | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000; // 1 secondo iniziale
  private isConnected = new BehaviorSubject<boolean>(false);
  public isConnected$ = this.isConnected.asObservable();

  private messageSubject = new Subject<WebSocketMessage>();
  public message$ = this.messageSubject.asObservable();

  constructor() {
    this.connect();
  }

  private connect(): void {
    try {
      // TODO: Implementare la connessione WebSocket reale
      // Per ora usiamo un placeholder
      console.log('Connecting to WebSocket...');
      
      // Simuliamo una connessione WebSocket per lo sviluppo
      this.simulateConnection();
    } catch (error) {
      console.error('WebSocket connection failed:', error);
      this.handleConnectionError();
    }
  }

  private simulateConnection(): void {
    // Simula una connessione WebSocket per lo sviluppo
    // In produzione, sostituire con la reale connessione WebSocket
    
    setTimeout(() => {
      this.isConnected.next(true);
      console.log('WebSocket connected (simulated)');
      
      // Simula ricezione di messaggi
      this.simulateMessageReception();
    }, 1000);
  }

  private simulateMessageReception(): void {
    // Simula la ricezione di messaggi dal server
    const mockMessages: WebSocketMessage[] = [
      {
        type: 'message',
        data: {
          content: 'Ciao! Come posso aiutarti oggi?',
          role: 'assistant',
          timestamp: new Date()
        },
        timestamp: new Date()
      },
      {
        type: 'tool-result',
        data: {
          toolName: 'get_projects',
          result: [
            { id: 1, name: 'Progetto A', status: 'in_progress' },
            { id: 2, name: 'Progetto B', status: 'completed' }
          ]
        },
        timestamp: new Date()
      }
    ];

    let messageIndex = 0;
    const interval = setInterval(() => {
      if (messageIndex < mockMessages.length && this.isConnected.value) {
        this.messageSubject.next(mockMessages[messageIndex]);
        messageIndex++;
      } else {
        clearInterval(interval);
      }
    }, 3000);
  }

  sendMessage(message: any): void {
    if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
      this.websocket.send(JSON.stringify(message));
    } else {
      console.warn('WebSocket non connesso, messaggio perso:', message);
    }
  }

  private handleConnectionError(): void {
    this.isConnected.next(false);
    
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      console.log(`Riconnessione in ${this.reconnectDelay / 1000} secondi...`);
      
      setTimeout(() => {
        this.reconnectAttempts++;
        this.connect();
      }, this.reconnectDelay);
      
      this.reconnectDelay *= 2; // Esponenziale backoff
    } else {
      console.error('Max reconnect attempts reached');
    }
  }

  disconnect(): void {
    if (this.websocket) {
      this.websocket.close();
      this.websocket = null;
    }
    this.isConnected.next(false);
  }

  // Metodi per la gestione dei tool calls
  sendToolCall(toolName: string, toolArguments: any): void {
    const message: WebSocketMessage = {
      type: 'tool-call',
      data: {
        toolName,
        arguments: toolArguments
      },
      timestamp: new Date()
    };
    
    this.sendMessage(message);
  }

  // Metodo per ricevere aggiornamenti in tempo reale
  onToolResult(callback: (result: any) => void): void {
    this.message$.subscribe(message => {
      if (message.type === 'tool-result') {
        callback(message.data);
      }
    });
  }

  // Metodo per ricevere errori in tempo reale
  onError(callback: (error: string) => void): void {
    this.message$.subscribe(message => {
      if (message.type === 'error') {
        callback(message.data);
      }
    });
  }
}