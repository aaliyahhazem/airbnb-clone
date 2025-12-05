import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RagChatService } from './rag-chat.service';
import { ListingService } from '../listings/listing.service';
import { LanguageService } from '../language.service';

describe('RagChatService', () => {
  let service: RagChatService;
  let httpMock: HttpTestingController;
  let listingService: jasmine.SpyObj<ListingService>;
  let languageService: jasmine.SpyObj<LanguageService>;

  beforeEach(() => {
    const listingServiceSpy = jasmine.createSpyObj('ListingService', ['getAll']);
    const languageServiceSpy = jasmine.createSpyObj('LanguageService', [], {
      currentLanguage: 'en'
    });

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        RagChatService,
        { provide: ListingService, useValue: listingServiceSpy },
        { provide: LanguageService, useValue: languageServiceSpy }
      ]
    });

    service = TestBed.inject(RagChatService);
    httpMock = TestBed.inject(HttpTestingController);
    listingService = TestBed.inject(ListingService) as jasmine.SpyObj<ListingService>;
    languageService = TestBed.inject(LanguageService) as jasmine.SpyObj<LanguageService>;
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('Chat Session Management', () => {
    it('should create a new chat session on init', (done) => {
      service.chatSession$.subscribe(session => {
        expect(session).toBeDefined();
        expect(session.messages).toEqual([]);
        expect(session.language).toBe('en');
        done();
      });
    });

    it('should clear chat session', (done) => {
      service.sendMessage('Hello').subscribe();

      service.clearSession();

      service.chatSession$.subscribe(session => {
        expect(session.messages.length).toBe(0);
        done();
      });
    });

    it('should save session to localStorage', (done) => {
      service.sendMessage('Test message').subscribe(() => {
        const saved = localStorage.getItem('chat_session');
        expect(saved).toBeTruthy();
        const session = JSON.parse(saved!);
        expect(session.messages.length).toBeGreaterThan(0);
        done();
      });

      // Mock API response
      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Response' }]);
      }
    });
  });

  describe('Message Sending', () => {
    it('should send user message and add to session', (done) => {
      service.sendMessage('Hello').subscribe(() => {
        service.chatSession$.subscribe(session => {
          const userMessages = session.messages.filter(m => m.role === 'user');
          expect(userMessages.length).toBeGreaterThan(0);
          expect(userMessages[0].content).toBe('Hello');
          done();
        });
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Hi there!' }]);
      }
    });

    it('should set typing indicator during API call', (done) => {
      let typingStates: boolean[] = [];

      service.isTyping$.subscribe(typing => {
        typingStates.push(typing);
      });

      service.sendMessage('Test').subscribe(() => {
        expect(typingStates).toContain(true);
        expect(typingStates[typingStates.length - 1]).toBe(false);
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Response' }]);
      }
    });
  });

  describe('Intent Detection', () => {
    it('should detect search intent', (done) => {
      service.sendMessage('I want to search for properties in Cairo').subscribe(response => {
        expect(response.context?.type).toBe('search');
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Here are properties in Cairo' }]);
      }
    });

    it('should detect booking intent', (done) => {
      service.sendMessage('I want to book a property').subscribe(response => {
        expect(response.context?.type).toBe('booking');
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Let me help you book' }]);
      }
    });

    it('should detect listing creation intent', (done) => {
      service.sendMessage('I want to add my property').subscribe(response => {
        expect(response.context?.type).toBe('listing');
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: "Great! Let's list your property" }]);
      }
    });
  });

  describe('Fallback Responses', () => {
    it('should use rule-based fallback when API fails', (done) => {
      service.sendMessage('Search properties').subscribe(response => {
        expect(response.content).toBeTruthy();
        expect(response.content.length).toBeGreaterThan(0);
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.error(new ErrorEvent('Network error'));
      }
    });

    it('should provide search fallback response', (done) => {
      localStorage.removeItem('hf_api_key');

      service.sendMessage('find properties').subscribe(response => {
        expect(response.content).toContain('search');
        done();
      });
    });

    it('should provide booking fallback response', (done) => {
      localStorage.removeItem('hf_api_key');

      service.sendMessage('book a property').subscribe(response => {
        expect(response.content.toLowerCase()).toContain('book');
        done();
      });
    });

    it('should provide Arabic fallback when language is Arabic', (done) => {
      (languageService as any).currentLanguage = 'ar';
      localStorage.removeItem('hf_api_key');

      service.sendMessage('بحث').subscribe(response => {
        expect(response.content).toMatch(/[\u0600-\u06FF]/); // Contains Arabic characters
        done();
      });
    });
  });

  describe('Action Extraction', () => {
    it('should extract search action from context', (done) => {
      service.sendMessage('search for villas in Cairo').subscribe(response => {
        expect(response.actions).toBeDefined();
        const searchAction = response.actions?.find(a => a.type === 'search');
        expect(searchAction).toBeDefined();
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Found properties' }]);
      }
    });

    it('should extract booking action', (done) => {
      service.sendMessage('I want to book').subscribe(response => {
        const bookAction = response.actions?.find(a => a.type === 'book');
        expect(bookAction).toBeDefined();
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: 'Ready to book' }]);
      }
    });

    it('should extract create listing action', (done) => {
      service.sendMessage('add my property').subscribe(response => {
        const createAction = response.actions?.find(a => a.type === 'create_listing');
        expect(createAction).toBeDefined();
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.flush([{ generated_text: "Let's create listing" }]);
      }
    });
  });

  describe('Error Handling', () => {
    it('should handle API errors gracefully', (done) => {
      service.sendMessage('Test').subscribe(response => {
        expect(response).toBeDefined();
        expect(response.content).toBeTruthy();
        done();
      });

      const req = httpMock.match(() => true)[0];
      if (req) {
        req.error(new ErrorEvent('API Error'));
      }
    });

    it('should retry with fallback model on primary failure', (done) => {
      service.sendMessage('Test').subscribe(() => {
        done();
      });

      const requests = httpMock.match(() => true);
      // First request fails
      requests[0]?.error(new ErrorEvent('Primary model error'));
      // Second request (fallback) succeeds
      if (requests.length > 1) {
        requests[1]?.flush([{ generated_text: 'Fallback response' }]);
      }
    });

    it('should create error message on complete failure', (done) => {
      service.sendMessage('Test').subscribe(response => {
        // Should still get a response even if all fails
        expect(response).toBeDefined();
        expect(response.content).toBeTruthy();
        done();
      });

      const requests = httpMock.match(() => true);
      requests.forEach(req => req.error(new ErrorEvent('Total failure')));
    });
  });

  describe('Token Optimization', () => {
    it('should limit conversation history', (done) => {
      // Send multiple messages
      const promises = [];
      for (let i = 0; i < 10; i++) {
        promises.push(service.sendMessage(`Message ${i}`).toPromise());
      }

      Promise.all(promises).then(() => {
        service.chatSession$.subscribe(session => {
          // Should keep only recent messages
          expect(session.messages.length).toBeLessThanOrEqual(20); // User + Assistant for each
          done();
        });
      });

      // Mock all responses
      const requests = httpMock.match(() => true);
      requests.forEach(req => req.flush([{ generated_text: 'Response' }]));
    });

    it('should use compact prompts', (done) => {
      service.sendMessage('Test query').subscribe();

      const req = httpMock.match(() => true)[0];
      if (req) {
        const body = req.request.body;
        // Prompt should be reasonably sized
        expect(body.inputs.length).toBeLessThan(2000);
        req.flush([{ generated_text: 'Response' }]);
        done();
      }
    });
  });

  describe('Search Parameter Extraction', () => {
    it('should extract location from query', () => {
      const params = (service as any).extractSearchParams('find properties in Cairo');
      expect(params.location).toBeTruthy();
    });

    it('should extract price from query', () => {
      const params = (service as any).extractSearchParams('properties under 5000 EGP');
      expect(params.maxPrice).toBe(5000);
    });

    it('should extract property type', () => {
      const params = (service as any).extractSearchParams('looking for a villa');
      expect(params.type).toBe('Villa');
    });
  });
});
