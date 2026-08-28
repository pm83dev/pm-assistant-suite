import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../../services/chat/chat.service';
import { ChatMessage, ChatResponse, ToolCall } from '../../models/models';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private destroy$ = new Subject<void>();

  messages: ChatMessage[] = [];
  currentMessage = '';
  isLoading = false;
  error: string | null = null;
  showTools = false;
  availableTools: any[] = [];

  ngOnInit(): void {
    this.loadChatHistory();
    this.loadAvailableTools();
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
      timestamp: new Date()
    };

    this.messages.push(userMessage);
    this.currentMessage = '';
    this.isLoading = true;
    this.error = null;

    this.chatService.sendMessage(messageText)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          const assistantMessage: ChatMessage = {
            role: 'assistant',
            content: response.content,
            timestamp: new Date(),
            toolCalls: response.toolCalls
          };

          this.messages.push(assistantMessage);
          this.isLoading = false;

          if (response.toolCalls && response.toolCalls.length > 0) {
            this.showTools = true;
          }
        },
        error: (err) => {
          this.error = 'Errore nell\'invio del messaggio';
          this.isLoading = false;
          console.error('Chat error:', err);
        }
      });
  }

  loadChatHistory(): void {
    this.chatService.getChatHistory()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (history) => {
          this.messages = history;
        },
        error: (err) => {
          console.error('Error loading chat history:', err);
        }
      });
  }

  loadAvailableTools(): void {
    this.chatService.getAvailableTools()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tools) => {
          this.availableTools = Object.entries(tools).map(([name, info]) => ({
            name,
            ...info
          }));
        },
        error: (err) => {
          console.error('Error loading tools:', err);
        }
      });
  }

  executeTool(toolName: string, toolArguments: string): void {
    this.isLoading = true;
    this.chatService.executeTool(toolName, toolArguments)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          // Aggiorna il messaggio corrente con il risultato del tool
          const lastMessage = this.messages[this.messages.length - 1];
          if (lastMessage && lastMessage.role === 'assistant') {
            lastMessage.content = result.content;
            lastMessage.toolCalls = result.toolCalls;
          }
          this.isLoading = false;
        },
        error: (err) => {
          this.error = 'Errore nell\'esecuzione del tool';
          this.isLoading = false;
          console.error('Tool execution error:', err);
        }
      });
  }

  toggleTools(): void {
    this.showTools = !this.showTools;
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