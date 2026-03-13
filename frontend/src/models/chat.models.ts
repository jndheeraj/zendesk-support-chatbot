export interface Message {
  id: string;
  text: string;
  sender: 'user' | 'bot';
  timestamp: string | Date;
}

export interface ChatState {
  isOpen: boolean;
  messages: Message[];
  isTyping: boolean;
}