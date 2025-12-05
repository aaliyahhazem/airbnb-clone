// Chat message models for RAG chatbot
export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  context?: RetrievedContext;
  actions?: ChatAction[];
  error?: boolean;
}

export interface RetrievedContext {
  type: 'listing' | 'booking' | 'search' | 'general';
  relevantListings?: any[];
  bookingInfo?: any;
  searchParams?: any;
  metadata?: any;
}

export interface ChatAction {
  type: 'search' | 'book' | 'create_listing' | 'navigate' | 'filter';
  label: string;
  data?: any;
}

export interface ChatSession {
  id: string;
  messages: ChatMessage[];
  userId?: number;
  language: 'en' | 'ar';
  createdAt: Date;
  updatedAt: Date;
}

export interface AIResponse {
  message: string;
  context?: RetrievedContext;
  actions?: ChatAction[];
  confidence?: number;
}

export interface ContextChunk {
  content: string;
  relevance: number;
  source: string;
  metadata?: any;
}
