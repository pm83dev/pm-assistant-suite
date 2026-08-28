import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ChatMessage } from '../../models/models';

export interface ChatSession {
  id: string;
  userId: string;
  title: string;
  createdAt: Date;
  updatedAt: Date;
  messages: ChatMessage[];
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ChatSessionService {
  private sessionsSubject = new BehaviorSubject<Map<string, ChatSession>>(new Map());
  public sessions$ = this.sessionsSubject.asObservable();

  private currentSessionSubject = new BehaviorSubject<ChatSession | null>(null);
  public currentSession$ = this.currentSessionSubject.asObservable();

  constructor() {
    this.loadSessions();
  }

  createSession(userId: string, title: string = 'Nuova Chat'): ChatSession {
    const session: ChatSession = {
      id: `session-${Date.now()}`,
      userId,
      title,
      createdAt: new Date(),
      updatedAt: new Date(),
      messages: [],
      isActive: true
    };

    const sessions = new Map(this.sessionsSubject.value);
    sessions.set(session.id, session);
    this.sessionsSubject.next(sessions);
    this.currentSessionSubject.next(session);

    return session;
  }

  addMessage(sessionId: string, message: ChatMessage): void {
    const sessions = new Map(this.sessionsSubject.value);
    const session = sessions.get(sessionId);

    if (session) {
      session.messages.push(message);
      session.updatedAt = new Date();
      sessions.set(sessionId, { ...session });
      this.sessionsSubject.next(sessions);
      this.currentSessionSubject.next({ ...session });
    }
  }

  updateSessionTitle(sessionId: string, title: string): void {
    const sessions = new Map(this.sessionsSubject.value);
    const session = sessions.get(sessionId);

    if (session) {
      session.title = title;
      session.updatedAt = new Date();
      sessions.set(sessionId, { ...session });
      this.sessionsSubject.next(sessions);
      this.currentSessionSubject.next({ ...session });
    }
  }

  deleteSession(sessionId: string): void {
    const sessions = new Map(this.sessionsSubject.value);
    sessions.delete(sessionId);
    this.sessionsSubject.next(sessions);

    // Se è la sessione corrente, crea una nuova
    const current = this.currentSessionSubject.value;
    if (current?.id === sessionId) {
      this.currentSessionSubject.next(null);
    }
  }

  getSession(sessionId: string): Observable<ChatSession | null> {
    return new Observable(observer => {
      const unsubscribe = this.sessions$.subscribe(sessions => {
        const session = sessions.get(sessionId);
        observer.next(session ?? null);
      });

      return () => unsubscribe.unsubscribe();
    });
  }

  getActiveSessions(userId: string): ChatSession[] {
    const sessions = Array.from(this.sessionsSubject.value.values());
    return sessions.filter(session => 
      session.userId === userId && session.isActive
    );
  }

  setCurrentSession(session: ChatSession | null): void {
    this.currentSessionSubject.next(session);
  }

  private loadSessions(): void {
    // TODO: Caricare le sessioni dal localStorage o dal server
    // Per ora inizializziamo con una sessione di prova
    const mockSession: ChatSession = {
      id: 'session-demo',
      userId: 'test-user-1',
      title: 'Demo Chat',
      createdAt: new Date(),
      updatedAt: new Date(),
      messages: [
        {
          role: 'assistant',
          content: 'Benvenuto nella demo chat! Questa è una sessione di esempio.',
          timestamp: new Date()
        }
      ],
      isActive: true
    };

    const sessions = new Map(this.sessionsSubject.value);
    sessions.set(mockSession.id, mockSession);
    this.sessionsSubject.next(sessions);
    this.currentSessionSubject.next(mockSession);
  }

  // Metodo per salvare le sessioni localmente
  private saveSessions(): void {
    // TODO: Implementare il salvataggio nel localStorage
    // Per ora non facciamo nulla
  }
}