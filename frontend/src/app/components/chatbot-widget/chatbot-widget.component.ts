import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Message } from '../../../models/chat.models';
import { ChatService } from '../../../services/chat.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import {trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-chatbot-widget',
  standalone: true,
  templateUrl: './chatbot-widget.component.html',
  styleUrls: ['./chatbot-widget.component.css'],
  imports: [
    FormsModule,
    CommonModule
  ],
  animations: [
    trigger('slideInOut', [
      state('in', style({ transform: 'translateY(0%)', opacity: 1 })),
      state('out', style({ transform: 'translateY(100%)', opacity: 0 })),
      transition('out => in', [animate('300ms ease-in')]),
      transition('in => out', [animate('200ms ease-out')]),
    ]),
    trigger('buttonScale', [
      state('normal', style({ transform: 'scale(1)' })),
      state('hover', style({ transform: 'scale(1.1)' })),
      transition('normal <=> hover', [animate('150ms ease-in-out')]),
    ]),
  ]
})
export class ChatbotWidgetComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;
  @ViewChild('messageInput') messageInput!: ElementRef;

  isOpen = false;
  messages: Message[] = [];
  inputValue = '';
  isTyping = false;
  buttonState = 'normal';
  shouldRefocus = false;

  constructor(private chatService: ChatService) {}

  ngOnInit() {
    this.messages = [this.chatService.getInitialMessage()];
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
    if (this.shouldRefocus && this.messageInput) {
      setTimeout(() => {
        this.messageInput.nativeElement.focus();
        this.shouldRefocus = false;
      }, 0);
    }
  }

  get buttonClasses(): string {
    return `w-14 h-14 rounded-full shadow-lg transition-all duration-300 ease-in-out
            flex items-center justify-center text-white font-medium
            hover:shadow-xl active:scale-95 ${
              this.isOpen ? 'bg-red-500 hover:bg-red-600' : 'bg-blue-600 hover:bg-blue-700'
            }`;
  }

  getMessageClasses(message: Message): string {
    return `max-w-xs px-4 py-2 rounded-2xl text-sm ${
      message.sender === 'user'
        ? 'bg-blue-600 text-white rounded-br-md'
        : 'bg-gray-100 text-gray-800 rounded-bl-md'
    }`;
  }

  toggleChat() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      setTimeout(() => this.messageInput?.nativeElement.focus(), 300);
    }
  }

  closeChat() {
    this.isOpen = false;
  }

  sendMessage() {
  if (!this.inputValue.trim()) return;

  const userMessage: Message = {
    id: Date.now().toString(),
    text: this.inputValue,
    sender: 'user',
    timestamp: new Date(),
  };

  this.messages.push(userMessage);
  const messageText = this.inputValue;
  this.inputValue = '';
  this.isTyping = true;
  this.shouldRefocus = true;


  this.chatService.sendMessage(messageText).subscribe({
    next: (response: any) => {
      const botMessage: Message = {
        id: response.id,
        text: response.text,
        sender: response.sender || 'bot',
        timestamp: response.timestamp,
      };
      this.messages.push(botMessage);
      this.isTyping = false;

      this.shouldRefocus = true;

    },
    error: (error) => {
      console.error('Error sending message:', error);
      this.isTyping = false;
      this.shouldRefocus = true;
    }
  });
}


  trackByMessageId(index: number, message: Message): string {
    return message.id;
  }

  private scrollToBottom() {
    if (this.messagesContainer) {
      const element = this.messagesContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }
}
