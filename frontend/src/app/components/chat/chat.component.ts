import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ChatMessage, ToolCall } from '../../models/models';
import { ChatService } from '../../services/chat/chat.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css'],
})
export class ChatComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private cdr = inject(ChangeDetectorRef);
  private destroy$ = new Subject<void>();

  // Debug: contatore per tracciare i messaggi caricati
  private messageLoadCount = 0;
  private lastKnownMessagesLength = 0;

  messages: ChatMessage[] = [];
  currentMessage = '';
  isLoading = false;
  error: string | null = null;
  showTools = false;
  availableTools: any[] = [];

  ngOnInit(): void {
    this.loadLocalChatHistory();
    this.loadChatHistory();
    this.loadAvailableTools();

    // Debug: monitora il caricamento dei messaggi
    setInterval(() => {
      if (this.messages.length !== this.lastKnownMessagesLength) {
        console.log(
          `[DEBUG] Messaggi caricati: ${this.messages.length} (variazione: ${this.messages.length - this.lastKnownMessagesLength})`,
        );
        this.lastKnownMessagesLength = this.messages.length;
      }
    }, 1000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  sendMessage(): void {
    if (!this.currentMessage.trim() || this.isLoading) return;

    const messageText = this.currentMessage;

    const userMessage: ChatMessage = {
      role: 'user',
      content: messageText,
      timestamp: new Date(),
    };

    this.messages.push(userMessage);
    this.chatService.addToChatHistory(userMessage);
    this.currentMessage = '';
    this.isLoading = true;
    this.error = null;

    // Aggiungi timeout di 30 secondi per prevenire caricamento infinito
    const timeout = setTimeout(() => {
      if (this.isLoading) {
        console.warn('Richiesta chat superata il timeout di 30 secondi');
        this.error = 'La richiesta è scaduta. Prova a ridurre la lunghezza del messaggio.';
        this.isLoading = false;
      }
    }, 30000);

    this.chatService
      .sendMessage(messageText)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          clearTimeout(timeout);

          // Debug: verifica che il contenuto non sia vuoto o problematico
          if (!response.content || response.content.trim() === '') {
            console.warn('[DEBUG] Risposta vuota dal server', response);
            // Se la risposta è vuota, non aggiungere nulla e termina il caricamento
            this.isLoading = false;
            return;
          }

          const assistantMessage: ChatMessage = {
            role: 'assistant',
            content: response.content,
            timestamp: new Date(),
            toolCalls: response.toolCalls || [],
          };

          this.isLoading = false;
          this.cdr.detectChanges();

          this.messages.push(assistantMessage);
          this.chatService.addToChatHistory(assistantMessage);
          this.isLoading = false;

          if (response.toolCalls && response.toolCalls.length > 0) {
            this.showTools = true;
          } else {
            this.showTools = false;
          }
        },
        error: (err) => {
          clearTimeout(timeout);
          this.error = "Errore nell'invio del messaggio";
          this.isLoading = false;
          console.error('Chat error:', err);
        },
      });
  }

  loadLocalChatHistory(): void {
    // Carica i messaggi locali se presenti
    const localMessages = this.chatService.getChatHistoryFromStorage();
    if (localMessages && localMessages.length > 0) {
      this.messages = localMessages;
    }
  }

  loadChatHistory(): void {
    this.chatService
      .getChatHistory()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (history) => {
          // Solo se non ci sono messaggi locali o se il backend restituisce una cronologia vuota
          if (!this.messages || this.messages.length === 0) {
            this.messages = history;
          }
        },
        error: (err) => {
          console.error('Error loading chat history:', err);
        },
      });
  }

  loadAvailableTools(): void {
    this.chatService
      .getAvailableTools()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tools) => {
          this.availableTools = Object.entries(tools).map(([name, info]) => ({
            name,
            ...info,
          }));
        },
        error: (err) => {
          console.error('Error loading tools:', err);
        },
      });
  }

  executeTool(toolName: string, toolArguments: string): void {
    this.isLoading = true;
    this.chatService
      .executeTool(toolName, toolArguments)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          // Aggiorna il messaggio corrente con il risultato del tool
          const lastMessage = this.messages[this.messages.length - 1];
          if (lastMessage && lastMessage.role === 'assistant') {
            lastMessage.content = result.content;
            lastMessage.toolCalls = result.toolCalls;
            this.chatService.updateChatHistory(this.messages);
          }
          this.isLoading = false;
        },
        error: (err) => {
          this.error = "Errore nell'esecuzione del tool";
          this.isLoading = false;
          console.error('Tool execution error:', err);
        },
      });
  }

  toggleTools(): void {
    this.showTools = !this.showTools;
  }

  resetChat(): void {
    this.messages = [];
    this.error = null;
    this.chatService.clearChatHistory();
  }

  formatToolCall(toolCall: ToolCall): string {
    try {
      const args = JSON.parse(toolCall.arguments);
      return `${toolCall.toolName}(${JSON.stringify(args, null, 2)})`;
    } catch {
      return `${toolCall.toolName}(${toolCall.arguments})`;
    }
  }
}
