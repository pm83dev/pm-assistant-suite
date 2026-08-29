import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { ChatMessage, ChatResponse, ToolStatusResponse } from '../../models/models';
import { environment } from '../../../environments/index';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly apiUrl = `${environment.apiPmAssistant}/api/chat`;
  private chatHistorySubject = new BehaviorSubject<ChatMessage[]>([]);
  public chatHistory$ = this.chatHistorySubject.asObservable();

  constructor(private http: HttpClient) { 
    // Carica la cronologia salvata in locale all'avvio
    this.loadChatHistoryFromStorage();
  }

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
    this.saveChatHistoryToStorage(messages);
  }

  // Metodo per aggiungere un messaggio alla cronologia
  addToChatHistory(message: ChatMessage): void {
    const currentMessages = this.chatHistorySubject.value;
    const newMessages = [...currentMessages, message];
    this.chatHistorySubject.next(newMessages);
    this.saveChatHistoryToStorage(newMessages);
  }

  // Carica la cronologia dalla memoria locale
  private loadChatHistoryFromStorage(): void {
    try {
      const storedHistory = localStorage.getItem('chatHistory');
      if (storedHistory) {
        const history = JSON.parse(storedHistory);
        this.chatHistorySubject.next(history);
      }
    } catch (error) {
      console.error('Errore nel caricamento della cronologia chat:', error);
    }
  }

  // Salva la cronologia nella memoria locale
  private saveChatHistoryToStorage(messages: ChatMessage[]): void {
    try {
      localStorage.setItem('chatHistory', JSON.stringify(messages));
    } catch (error) {
      console.error('Errore nel salvataggio della cronologia chat:', error);
    }
  }

  // Metodo per ottenere la cronologia dallo storage
  getChatHistoryFromStorage(): ChatMessage[] | null {
    try {
      const storedHistory = localStorage.getItem('chatHistory');
      return storedHistory ? JSON.parse(storedHistory) : null;
    } catch (error) {
      console.error('Errore nel recupero della cronologia chat dallo storage:', error);
      return null;
    }
  }

  // Metodo per azzerare la cronologia (locale e in storage)
  clearChatHistory(): void {
    this.chatHistorySubject.next([]);
    try {
      localStorage.removeItem('chatHistory');
    } catch (error) {
      console.error('Errore nella cancellazione della cronologia chat dallo storage:', error);
    }
  }
}