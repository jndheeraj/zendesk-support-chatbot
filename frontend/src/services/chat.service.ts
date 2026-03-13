import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';
import { Message } from '../models/chat.models';
import { HttpClient } from '@angular/common/http';
import { AppConfigService } from './app-config.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private botResponses = [
    "Thank you for your message! I'm here to help you with any questions.",
    "I understand your concern. Let me assist you with that.",
    "That's a great question! Here's what I can tell you...",
    "I'd be happy to help you resolve this issue.",
    "Let me connect you with the right information for that.",
  ];

  

  constructor(private configService: AppConfigService, private http: HttpClient) {}

sendMessage(message: string): Observable<Message> {
  const fullUrl = `${this.configService.apiUrl}/api/chat`;
  const payload = {
    text: message,
    sender: 'user'
  };

  return this.http.post<Message>(
    fullUrl,
    payload,
    { headers: { 'Content-Type': 'application/json', 'X-Business': 'your-business' } }
  );
}

  // sendMessage(message: string): Observable<Message> {
  //   const randomResponse = this.botResponses[Math.floor(Math.random() * this.botResponses.length)];
    
  //   const botMessage: Message = {
  //     id: (Date.now() + 1).toString(),
  //     text: randomResponse,
  //     sender: 'bot',
  //     timestamp: new Date(),
  //   };

  //   return of(botMessage).pipe(delay(1500));
  // }

  getInitialMessage(): Message {
    return {
      id: '1',
      text: "Hello! I'm your support assistant. How can I help you today?",
      sender: 'bot',
      timestamp: new Date(),
    };
  }
}