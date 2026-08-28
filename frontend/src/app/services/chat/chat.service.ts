import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { ChatMessage, ChatResponse, ToolStatusResponse } from '../../models/models';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = '/api/chat';
  private chatHistorySubject = new BehaviorSubject<ChatMessage[]>([]);
  public chatHistory$ = this.chatHistorySubject.asObservable();

  constructor(private http: HttpClient) { }

  sendMessage(message: string): Observable<ChatResponse> {
    const userId = this.getCurrentUserId();
    return this.http.post<ChatResponse>(`${this.apiUrl}/message`, {
      message,
      userId,
      tools: [] // TODO: Passare i tool selezionati
    });
  }

  getChatHistory(): Observable<ChatMessage[]> {
    const userId = this.getCurrentUserId();
    return this.http.get<ChatMessage[]>(`${this.apiUrl}/history/${userId}`);
  }

  getAvailableTools(): Observable<ToolStatusResponse> {
    return this.http.get<ToolStatusResponse>(`${this.apiUrl}/tools/status`);
  }

  executeTool(toolName: string, toolArguments: string): Observable<any> {
    const userId = this.getCurrentUserId();
    return this.http.post<any>(`${this.apiUrl}/execute-tool`, {
      toolName,
      arguments: toolArguments,
      userId
    });
  }

  private getCurrentUserId(): string {
    // TODO: Implementare il recupero dell'ID utente attuale
    // Per ora restituiamo un ID di test
    return 'test-user-1';
  }

  // Metodo per aggiornare la cronologia localmente
  updateChatHistory(messages: ChatMessage[]): void {
    this.chatHistorySubject.next(messages);
  }

  // Metodo per aggiungere un messaggio alla cronologia
  addToChatHistory(message: ChatMessage): void {
    const currentMessages = this.chatHistorySubject.value;
    this.chatHistorySubject.next([...currentMessages, message]);
  }
}