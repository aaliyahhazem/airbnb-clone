import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, map, retry, tap, debounceTime } from 'rxjs/operators';
import { ChatMessage, ChatSession, AIResponse, ChatAction, RetrievedContext } from '../../models/chat.model';
import { ListingService } from '../listings/listing.service';
import { LanguageService } from '../language.service';

@Injectable({
  providedIn: 'root'
})
export class RagChatService {
  private http = inject(HttpClient);
  private listingService = inject(ListingService);
  private languageService = inject(LanguageService);

  // Chat state
  private chatSessionSubject = new BehaviorSubject<ChatSession>(this.createNewSession());
  public chatSession$ = this.chatSessionSubject.asObservable();

  private isTypingSubject = new BehaviorSubject<boolean>(false);
  public isTyping$ = this.isTypingSubject.asObservable();

  private chatOpenSubject = new BehaviorSubject<boolean>(false);
  public chatOpen$ = this.chatOpenSubject.asObservable();

  // Store current language
  private currentLang: 'en' | 'ar' = 'en';

  // API Configuration - CORS-friendly approach
  // NOTE: Hugging Face has CORS restrictions, so we'll use fallback primarily
  // For production, proxy through your backend or use Groq API
  private readonly API_BASE = 'https://api-inference.huggingface.co/models';
  private readonly MODEL = 'mistralai/Mixtral-8x7B-Instruct-v0.1';
  private readonly FALLBACK_MODEL = 'microsoft/DialoGPT-medium';
  private readonly USE_API = false; // Set to false to use rule-based fallback (CORS issue)

  // Token optimization
  private readonly MAX_CONTEXT_TOKENS = 500;
  private readonly MAX_HISTORY_MESSAGES = 6;

  constructor() {
    // Subscribe to language changes
    this.languageService.currentLanguage$.subscribe(lang => {
      this.currentLang = lang as 'en' | 'ar';
    });

    // Load session from localStorage
    this.loadSession();
  }

  /**
   * Send a message and get RAG-enhanced response
   */
  sendMessage(userMessage: string): Observable<ChatMessage> {
    console.log('📨 Sending message:', userMessage);
    const userMsg = this.createUserMessage(userMessage);
    this.addMessageToSession(userMsg);
    this.isTypingSubject.next(true);

    // 1. Retrieve relevant context
    const context = this.retrieveContext(userMessage);
    console.log('🔍 Context retrieved:', context);

    // 2. Build optimized prompt
    const prompt = this.buildOptimizedPrompt(userMessage, context);

    // 3. Use rule-based response (CORS workaround)
    // For production: proxy API calls through your backend
    const fallbackResponse = this.getFallbackResponse(userMessage);
    console.log('💬 Fallback response:', fallbackResponse);

    return of(fallbackResponse).pipe(
      map(fallbackText => {
        const processed = this.processAIResponse(fallbackText, context);
        console.log('✅ Processed response:', processed);
        return processed;
      }),
      tap(assistantMsg => {
        console.log('➕ Adding to session:', assistantMsg);
        this.addMessageToSession(assistantMsg);
        this.isTypingSubject.next(false);
        this.saveSession();
      }),
      catchError(error => {
        console.error('❌ Error:', error);
        this.isTypingSubject.next(false);
        return of(this.createErrorMessage(error));
      })
    );
  }

  /**
   * Retrieve relevant context from listings database
   */
  private retrieveContext(query: string): RetrievedContext {
    const lowerQuery = query.toLowerCase();
    const context: RetrievedContext = {
      type: 'general'
    };

    // Use advanced semantic extraction
    const semanticData = this.extractSemanticIntent(lowerQuery);
    
    // Detect intent with confidence scoring
    const intent = this.detectIntentWithConfidence(lowerQuery, semanticData);

    // Set context based on detected intent
    if (intent === 'search') {
      context.type = 'search';
      context.searchParams = this.buildSearchParamsFromSemantic(semanticData);
      context.relevantListings = this.getRelevantListings(context.searchParams);
    } else if (intent === 'booking') {
      context.type = 'booking';
      context.bookingInfo = this.buildBookingInfoFromSemantic(semanticData);
    } else if (intent === 'listing') {
      context.type = 'listing';
    }

    return context;
  }

  /**
   * Build search params from semantic data
   */
  private buildSearchParamsFromSemantic(semanticData: any): any {
    const params: any = {};

    if (semanticData.locations.length > 0) {
      params.destination = semanticData.locations[0];
    }

    if (semanticData.prices.length > 0) {
      params.maxPrice = Math.max(...semanticData.prices);
    }

    if (semanticData.bedrooms !== null) {
      params.minBedrooms = semanticData.bedrooms;
    }

    if (semanticData.propertyTypes.length > 0) {
      params.type = semanticData.propertyTypes[0];
    }

    return params;
  }

  /**
   * Build booking info from semantic data
   */
  private buildBookingInfoFromSemantic(semanticData: any): any {
    const info: any = {};

    if (semanticData.locations.length > 0) {
      info.location = semanticData.locations[0];
    }

    if (semanticData.timeframe) {
      info.timeframe = semanticData.timeframe;
    }

    return info;
  }

  /**
   * Build optimized prompt to minimize tokens
   */
  private buildOptimizedPrompt(userMessage: string, context: RetrievedContext): string {
    const lang = this.currentLang;
    const systemPrompt = this.getSystemPrompt(lang);
    const contextStr = this.summarizeContext(context);
    const history = this.getRecentHistory();

    // Compact prompt format
    return `${systemPrompt}

Context: ${contextStr}

History:
${history}

User: ${userMessage}
Assistant:`;
  }

  /**
   * Get optimized system prompt
   */
  private getSystemPrompt(lang: string): string {
    if (lang === 'ar') {
      return `أنت "السمسارة" - مساعد عقارات مصري خبير. ساعد المستخدمين في:
1. البحث عن عقارات
2. الحجز
3. إضافة عقارات
4. الأسئلة العامة

كن مختصراً ومفيداً. استخدم العربية المصرية. قدم إجراءات واضحة.`;
    }

    return `You are "The Broker" - an expert Egyptian property assistant. Help users:
1. Search properties
2. Book stays
3. List properties
4. Answer questions

Be concise, helpful. Suggest clear actions.`;
  }

  /**
   * Call AI API with retry and fallback
   */
  private callAIAPI(prompt: string, retryCount = 0): Observable<string> {
    const apiKey = this.getAPIKey();

    if (!apiKey) {
      return of(this.getFallbackResponse(prompt));
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${apiKey}`,
      'Content-Type': 'application/json'
    });

    const model = retryCount === 0 ? this.MODEL : this.FALLBACK_MODEL;

    return this.http.post<any>(
      `${this.API_BASE}/${model}`,
      {
        inputs: prompt,
        parameters: {
          max_new_tokens: 150, // Minimize tokens
          temperature: 0.7,
          top_p: 0.9,
          return_full_text: false
        }
      },
      { headers }
    ).pipe(
      retry(2),
      map(response => {
        if (Array.isArray(response)) {
          return response[0]?.generated_text || response[0]?.text || '';
        }
        return response.generated_text || response[0]?.generated_text || '';
      }),
      catchError((error: HttpErrorResponse) => {
        if (retryCount < 1) {
          // Try fallback model
          return this.callAIAPI(prompt, retryCount + 1);
        }
        // Use rule-based fallback
        return of(this.getFallbackResponse(prompt));
      })
    );
  }

  /**
   * Advanced NLP-like fallback with semantic understanding
   */
  private getFallbackResponse(userMessage: string): string {
    const lang = this.currentLang;
    const lower = userMessage.toLowerCase();
    
    // Advanced semantic extraction
    const semanticData = this.extractSemanticIntent(lower);
    
    // Detect intent with confidence scoring
    const intent = this.detectIntentWithConfidence(lower, semanticData);
    
    console.log('🧠 Semantic Analysis:', { intent, semanticData });
    
    return this.generateIntelligentResponse(intent, semanticData, lang);
  }

  /**
   * Extract semantic data from user input using advanced patterns
   */
  private extractSemanticIntent(text: string): any {
    const data: any = {
      locations: [],
      prices: [],
      bedrooms: null,
      bathrooms: null,
      propertyTypes: [],
      amenities: [],
      timeframe: null,
      action: null
    };

    // Location extraction - comprehensive patterns
    const locationPatterns = [
      /(?:in|at|near|around|close to|في|قرب|حوالي)\s+([a-z\u0600-\u06FF\s]+?)(?:\s|,|\.|\?|$)/gi,
      /([a-z\u0600-\u06FF]+)\s+(?:area|district|city|neighborhood|منطقة|حي|مدينة)/gi,
      /(cairo|alex|maadi|zamalek|downtown|coast|marina|القاهرة|الاسكندرية|المعادي|الزمالك|وسط البلد|الساحل|مارينا)/gi
    ];
    locationPatterns.forEach(pattern => {
      const matches = [...text.matchAll(pattern)];
      matches.forEach(m => {
        const loc = m[1]?.trim();
        if (loc && loc.length > 2) data.locations.push(loc);
      });
    });

    // Price extraction - multiple formats
    const pricePatterns = [
      /(\d+(?:,\d+)*(?:\.\d+)?)\s*(?:k|thousand|ألف)/gi,  // 5k, 5 thousand
      /(?:under|below|less than|max|maximum|أقل من|تحت|حد أقصى)\s*(\d+(?:,\d+)*)/gi,
      /(\d+(?:,\d+)*)\s*(?:egp|جنيه|pound|dollar)/gi,
      /(?:budget|price|cost|سعر|تكلفة|ميزانية).*?(\d+(?:,\d+)*)/gi,
      /(\d+)\s*(?:-|to|إلى)\s*(\d+)/gi  // range: 1000-5000
    ];
    pricePatterns.forEach(pattern => {
      const matches = [...text.matchAll(pattern)];
      matches.forEach(m => {
        const price1 = parseInt(m[1]?.replace(/,/g, ''));
        const price2 = m[2] ? parseInt(m[2].replace(/,/g, '')) : null;
        if (price1) data.prices.push(price1);
        if (price2) data.prices.push(price2);
      });
    });

    // Bedroom/bathroom extraction
    const bedroomPatterns = [
      /(\d+)\s*(?:bed|bedroom|غرف نوم|غرفة نوم|غرف|br)/gi,
      /(\d+)(?:\s|-)?(?:bed|br)/gi,
      /(?:studio|استوديو)/gi  // Studio = 0 bedrooms
    ];
    bedroomPatterns.forEach(pattern => {
      const match = text.match(pattern);
      if (match) {
        if (/studio|استوديو/i.test(match[0])) {
          data.bedrooms = 0;
        } else {
          const num = parseInt(match[1]);
          if (!isNaN(num)) data.bedrooms = num;
        }
      }
    });

    const bathroomPatterns = [
      /(\d+)\s*(?:bath|bathroom|حمام|حمامات|ba)/gi
    ];
    bathroomPatterns.forEach(pattern => {
      const match = text.match(pattern);
      if (match && match[1]) {
        const num = parseInt(match[1]);
        if (!isNaN(num)) data.bathrooms = num;
      }
    });

    // Property type extraction - comprehensive
    const propertyTypeMap = {
      apartment: /apartment|flat|condo|unit|شقة|وحدة/gi,
      villa: /villa|mansion|فيلا|قصر/gi,
      house: /house|home|townhouse|منزل|بيت|دار/gi,
      penthouse: /penthouse|rooftop|بنتهاوس|روف/gi,
      studio: /studio|استوديو/gi,
      duplex: /duplex|دوبلكس/gi,
      chalet: /chalet|شاليه/gi,
      office: /office|commercial|مكتب|تجاري/gi
    };
    Object.entries(propertyTypeMap).forEach(([type, pattern]) => {
      if (pattern.test(text)) data.propertyTypes.push(type);
    });

    // Amenities extraction
    const amenityMap = {
      pool: /pool|swimming|حمام سباحة|مسبح/gi,
      garden: /garden|yard|backyard|حديقة/gi,
      parking: /parking|garage|موقف|جراج/gi,
      furnished: /furnished|مفروش/gi,
      balcony: /balcony|terrace|شرفة|بلكونة/gi,
      elevator: /elevator|lift|أسانسير|مصعد/gi,
      security: /security|guard|حراسة|أمن/gi,
      gym: /gym|fitness|جيم|رياضة/gi,
      ac: /ac|air.?condition|تكييف/gi,
      kitchen: /kitchen|مطبخ/gi
    };
    Object.entries(amenityMap).forEach(([amenity, pattern]) => {
      if (pattern.test(text)) data.amenities.push(amenity);
    });

    // Timeframe detection
    if (/today|now|urgent|immediate|اليوم|الآن|عاجل|فوري/gi.test(text)) {
      data.timeframe = 'immediate';
    } else if (/week|أسبوع/gi.test(text)) {
      data.timeframe = 'week';
    } else if (/month|شهر/gi.test(text)) {
      data.timeframe = 'month';
    }

    // Action detection
    if (/show|display|view|list|see|عرض|اعرض|شوف/gi.test(text)) {
      data.action = 'view';
    } else if (/compare|مقارنة/gi.test(text)) {
      data.action = 'compare';
    } else if (/recommend|suggest|نصح|اقترح/gi.test(text)) {
      data.action = 'recommend';
    }

    return data;
  }

  /**
   * Detect intent with confidence scoring
   */
  private detectIntentWithConfidence(text: string, semanticData: any): string {
    const scores = {
      search: 0,
      booking: 0,
      listing: 0,
      question: 0,
      greeting: 0
    };

    // Search intent signals
    const searchSignals: (RegExp | number)[] = [
      /(?:search|find|looking|need|want|show|display|بحث|دور|ابحث|عايز|محتاج)/gi,
      semanticData.locations.length > 0 ? 1 : 0,
      semanticData.prices.length > 0 ? 1 : 0,
      semanticData.bedrooms !== null ? 1 : 0,
      semanticData.propertyTypes.length > 0 ? 1 : 0,
      semanticData.amenities.length > 0 ? 0.5 : 0
    ];
    scores.search = searchSignals.reduce((sum: number, signal) => {
      return sum + (typeof signal === 'number' ? signal : signal.test(text) ? 2 : 0);
    }, 0);

    // Booking intent signals
    const bookingSignals: (RegExp | number)[] = [
      /(?:book|reserve|rent|stay|check.?in|حجز|احجز|استئجار|إقامة)/gi,
      semanticData.timeframe ? 2 : 0,
      /(?:night|week|month|ليلة|أسبوع|شهر)/gi.test(text) ? 1 : 0
    ];
    scores.booking = bookingSignals.reduce((sum: number, signal) => {
      return sum + (typeof signal === 'number' ? signal : signal.test(text) ? 3 : 0);
    }, 0);

    // Listing intent signals
    const listingSignals: (RegExp | number)[] = [
      /(?:list|add|sell|host|my property|create|إضافة|بيع|عقاري|اضافة|مضيف)/gi,
      /(?:i have|i own|my|لدي|عندي)/gi.test(text) ? 2 : 0
    ];
    scores.listing = listingSignals.reduce((sum: number, signal) => {
      return sum + (typeof signal === 'number' ? signal : signal.test(text) ? 3 : 0);
    }, 0);

    // Question intent signals
    const questionSignals: (RegExp | number)[] = [
      /(?:how|what|when|where|why|which|كيف|ماذا|متى|أين|لماذا|أي)/gi,
      /(?:help|assist|info|مساعدة|معلومات)/gi,
      /\?/.test(text) ? 1 : 0
    ];
    scores.question = questionSignals.reduce((sum: number, signal) => {
      return sum + (typeof signal === 'number' ? signal : signal.test(text) ? 2 : 0);
    }, 0);

    // Greeting signals
    if (/^(hi|hello|hey|morning|evening|مرحبا|السلام|أهلا|صباح|مساء)[\s!.]*$/i.test(text.trim())) {
      scores.greeting = 10;
    }

    // Get highest scoring intent
    const maxScore = Math.max(...Object.values(scores));
    const intent = Object.entries(scores).find(([_, score]) => score === maxScore)?.[0] || 'general';

    console.log('🎯 Intent Scores:', scores, '→', intent);

    return intent;
  }

  /**
   * Generate intelligent response based on intent and semantic data
   */
  private generateIntelligentResponse(intent: string, data: any, lang: 'en' | 'ar'): string {
    // SEARCH INTENT
    if (intent === 'search') {
      return this.generateSearchResponse(data, lang);
    }

    // BOOKING INTENT
    if (intent === 'booking') {
      return this.generateBookingResponse(data, lang);
    }

    // LISTING INTENT
    if (intent === 'listing') {
      return this.generateListingResponse(data, lang);
    }

    // QUESTION INTENT
    if (intent === 'question') {
      return this.generateQuestionResponse(data, lang);
    }

    // GREETING
    if (intent === 'greeting') {
      return lang === 'ar'
        ? 'مرحباً! 👋 أنا السمسارة. ماذا تبحث عن؟\n• "ابحث عن شقة"\n• "أحجز فيلا"\n• "أضف عقاري"'
        : 'Hello! 👋 I\'m The Broker. What are you looking for?\n• "Search for apartment"\n• "Book a villa"\n• "List my property"';
    }

    // GENERAL - try to be helpful
    return this.generateGeneralResponse(data, lang);
  }

  /**
   * Generate search-specific response
   */
  private generateSearchResponse(data: any, lang: 'en' | 'ar'): string {
    const criteria = [];
    let propertyType = lang === 'ar' ? 'عقارات' : 'properties';

    // Property type
    if (data.propertyTypes.length > 0) {
      const type = data.propertyTypes[0];
      const typeMap: any = {
        en: { apartment: 'apartments', villa: 'villas', house: 'houses', studio: 'studios', penthouse: 'penthouses', chalet: 'chalets' },
        ar: { apartment: 'شقق', villa: 'فلل', house: 'منازل', studio: 'استوديوهات', penthouse: 'بنتهاوس', chalet: 'شاليهات' }
      };
      propertyType = typeMap[lang][type] || propertyType;
    }

    if (lang === 'ar') {
      let response = `تم! سأبحث عن ${propertyType}`;
      
      // Location
      if (data.locations.length > 0) {
        response += ` في ${data.locations[0]}`;
        criteria.push(`📍 الموقع: ${data.locations[0]}`);
      }
      
      // Bedrooms
      if (data.bedrooms !== null) {
        response += ` بـ ${data.bedrooms} ${data.bedrooms === 0 ? 'استوديو' : 'غرف نوم'}`;
        criteria.push(`🛏️ غرف النوم: ${data.bedrooms}`);
      }
      
      // Bathrooms
      if (data.bathrooms !== null) {
        criteria.push(`🚿 حمامات: ${data.bathrooms}`);
      }
      
      // Price
      if (data.prices.length > 0) {
        const maxPrice = Math.max(...data.prices);
        response += ` تحت ${maxPrice.toLocaleString()} جنيه`;
        criteria.push(`💰 السعر الأقصى: ${maxPrice.toLocaleString()} جنيه`);
      }
      
      // Amenities
      if (data.amenities.length > 0) {
        const amenityNames: any = {
          pool: 'مسبح', garden: 'حديقة', parking: 'موقف', furnished: 'مفروش',
          balcony: 'شرفة', elevator: 'مصعد', security: 'حراسة', gym: 'جيم', ac: 'تكييف'
        };
        data.amenities.forEach((a: string) => {
          criteria.push(`✨ ${amenityNames[a] || a}`);
        });
      }
      
      response += '.';
      
      if (criteria.length > 0) {
        response += `\n\n📋 معايير البحث:\n${criteria.join('\n')}`;
      }
      
      response += '\n\n✨ اضغط "عرض النتائج" للبحث الآن!';
      return response;
      
    } else {
      let response = `Perfect! I'll search for ${propertyType}`;
      
      if (data.locations.length > 0) {
        response += ` in ${data.locations[0]}`;
        criteria.push(`📍 Location: ${data.locations[0]}`);
      }
      
      if (data.bedrooms !== null) {
        response += ` with ${data.bedrooms} ${data.bedrooms === 0 ? 'studio' : 'bedrooms'}`;
        criteria.push(`🛏️ Bedrooms: ${data.bedrooms}`);
      }
      
      if (data.bathrooms !== null) {
        criteria.push(`🚿 Bathrooms: ${data.bathrooms}`);
      }
      
      if (data.prices.length > 0) {
        const maxPrice = Math.max(...data.prices);
        response += ` under ${maxPrice.toLocaleString()} EGP`;
        criteria.push(`💰 Max Price: ${maxPrice.toLocaleString()} EGP`);
      }
      
      if (data.amenities.length > 0) {
        data.amenities.forEach((a: string) => {
          criteria.push(`✨ ${a.charAt(0).toUpperCase() + a.slice(1)}`);
        });
      }
      
      response += '.';
      
      if (criteria.length > 0) {
        response += `\n\n📋 Search Criteria:\n${criteria.join('\n')}`;
      }
      
      response += '\n\n✨ Click "View Results" to search now!';
      return response;
    }
  }

  /**
   * Generate booking-specific response
   */
  private generateBookingResponse(data: any, lang: 'en' | 'ar'): string {
    const details = [];
    
    if (lang === 'ar') {
      let response = 'عظيم! سأساعدك في الحجز';
      
      if (data.locations.length > 0) {
        response += ` في ${data.locations[0]}`;
        details.push(`📍 ${data.locations[0]}`);
      }
      
      if (data.timeframe) {
        const timeMap: any = { immediate: 'فوري', week: 'أسبوع', month: 'شهر' };
        details.push(`📅 ${timeMap[data.timeframe]}`);
      }
      
      if (data.prices.length > 0) {
        details.push(`💰 ميزانية: ${Math.max(...data.prices).toLocaleString()} جنيه`);
      }
      
      response += '.';
      
      if (details.length > 0) {
        response += `\n\n${details.join(' • ')}`;
      }
      
      response += '\n\n🏠 اضغط "ابدأ الحجز" للمتابعة!';
      return response;
      
    } else {
      let response = 'Great! I\'ll help you book';
      
      if (data.locations.length > 0) {
        response += ` in ${data.locations[0]}`;
        details.push(`📍 ${data.locations[0]}`);
      }
      
      if (data.timeframe) {
        const timeMap: any = { immediate: 'immediately', week: 'this week', month: 'this month' };
        details.push(`📅 ${timeMap[data.timeframe]}`);
      }
      
      if (data.prices.length > 0) {
        details.push(`💰 Budget: ${Math.max(...data.prices).toLocaleString()} EGP`);
      }
      
      response += '.';
      
      if (details.length > 0) {
        response += `\n\n${details.join(' • ')}`;
      }
      
      response += '\n\n🏠 Click "Start Booking" to proceed!';
      return response;
    }
  }

  /**
   * Generate listing-specific response
   */
  private generateListingResponse(data: any, lang: 'en' | 'ar'): string {
    const propertyInfo = [];
    
    if (data.propertyTypes.length > 0) {
      const typeMap: any = {
        en: { apartment: 'apartment', villa: 'villa', house: 'house' },
        ar: { apartment: 'شقة', villa: 'فيلا', house: 'منزل' }
      };
      propertyInfo.push(typeMap[lang][data.propertyTypes[0]] || (lang === 'ar' ? 'عقار' : 'property'));
    }
    
    if (data.bedrooms !== null) {
      propertyInfo.push(lang === 'ar' ? `${data.bedrooms} غرف` : `${data.bedrooms} beds`);
    }
    
    if (data.locations.length > 0) {
      propertyInfo.push(lang === 'ar' ? `في ${data.locations[0]}` : `in ${data.locations[0]}`);
    }
    
    if (lang === 'ar') {
      let response = `رائع! تريد إضافة ${propertyInfo.join(' ')}. `;
      response += '\n\nسأرشدك خلال عملية الإضافة:\n• 📸 صور واضحة\n• 📝 التفاصيل\n• 💰 السعر\n• ✨ المرافق';
      response += '\n\nالعملية تستغرق 5 دقائق فقط! ⚡\n\nاضغط "إضافة عقار" للبدء.';
      return response;
    } else {
      let response = `Excellent! You want to list your ${propertyInfo.join(' ')}. `;
      response += '\n\nI\'ll guide you through:\n• 📸 Clear photos\n• 📝 Details\n• 💰 Pricing\n• ✨ Amenities';
      response += '\n\nTakes only 5 minutes! ⚡\n\nClick "Create Listing" to start.';
      return response;
    }
  }

  /**
   * Generate question-specific response
   */
  private generateQuestionResponse(data: any, lang: 'en' | 'ar'): string {
    return lang === 'ar'
      ? '🤝 يمكنني مساعدتك في:\n\n1️⃣ البحث السريع\nمثال: "شقة 3 غرف في القاهرة تحت 4000 جنيه"\n\n2️⃣ الحجز الفوري\nمثال: "احجز فيلا في الساحل"\n\n3️⃣ إضافة عقارك\nمثال: "أضف شقتي للإيجار"\n\nجرب الآن! 🚀'
      : '🤝 I can help you with:\n\n1️⃣ Quick Search\nExample: "3-bed apartment in Cairo under 4000 EGP"\n\n2️⃣ Instant Booking\nExample: "Book villa in North Coast"\n\n3️⃣ List Your Property\nExample: "List my apartment for rent"\n\nTry now! 🚀';
  }

  /**
   * Generate general helpful response
   */
  private generateGeneralResponse(data: any, lang: 'en' | 'ar'): string {
    // If we detected ANY useful data, try to be smart about it
    if (data.locations.length > 0 || data.prices.length > 0 || data.propertyTypes.length > 0) {
      return this.generateSearchResponse(data, lang);
    }
    
    return lang === 'ar'
      ? '🤔 جرب أن تقول:\n• "شقة 2 غرفة في المعادي"\n• "فيلا مع مسبح"\n• "عقارات تحت 5000 جنيه في القاهرة"\n\nأو اسألني مباشرة! 💬'
      : '🤔 Try saying:\n• "2-bed apartment in Maadi"\n• "Villa with pool"\n• "Properties under 5000 EGP in Cairo"\n\nOr just ask me directly! 💬';
  }

  /**
   * Process AI response and extract actions
   */
  private processAIResponse(aiText: string, context: RetrievedContext): ChatMessage {
    const actions = this.extractActions(aiText, context);

    return {
      id: this.generateId(),
      role: 'assistant',
      content: this.cleanAIResponse(aiText),
      timestamp: new Date(),
      context,
      actions
    };
  }

  /**
   * Extract actionable items from response
   */
  private extractActions(text: string, context: RetrievedContext): ChatAction[] {
    const actions: ChatAction[] = [];
    const lang = this.currentLang;

    // Always show search action for search intent
    if (context.type === 'search') {
      actions.push({
        type: 'search',
        label: lang === 'ar' ? 'عرض النتائج' : 'View Results',
        data: context.searchParams
      });
    }

    if (context.type === 'booking') {
      actions.push({
        type: 'book',
        label: lang === 'ar' ? 'ابدأ الحجز' : 'Start Booking',
        data: context.bookingInfo
      });
    }

    if (context.type === 'listing') {
      actions.push({
        type: 'create_listing',
        label: lang === 'ar' ? 'إضافة عقار' : 'Create Listing',
        data: {}
      });
    }

    return actions;
  }

  /**
   * Intent detection helpers
   */
  private isSearchIntent(query: string): boolean {
    const searchKeywords = [
      'search', 'find', 'looking for', 'show me', 'need', 'want',
      'بحث', 'دور', 'عايز', 'محتاج', 'اريد', 'ابحث'
    ];
    return searchKeywords.some(kw => query.includes(kw));
  }

  private isBookingIntent(query: string): boolean {
    const bookingKeywords = [
      'book', 'reserve', 'reservation', 'stay', 'check in',
      'حجز', 'احجز', 'حجوزات', 'إقامة'
    ];
    return bookingKeywords.some(kw => query.includes(kw));
  }

  private isListingCreationIntent(query: string): boolean {
    const listingKeywords = [
      'list my', 'add property', 'create listing', 'become host',
      'إضافة عقار', 'عقاري', 'مضيف', 'اضافة'
    ];
    return listingKeywords.some(kw => query.includes(kw));
  }

  /**
   * Extract search parameters from query
   */
  private extractSearchParams(query: string): any {
    const params: any = {};

    // Extract location
    const locationMatch = query.match(/in\s+([a-z\s]+)|في\s+([^\s]+)/i);
    if (locationMatch) {
      params.location = locationMatch[1] || locationMatch[2];
    }

    // Extract price
    const priceMatch = query.match(/(\d+)\s*(egp|جنيه|dollar)/i);
    if (priceMatch) {
      params.maxPrice = parseInt(priceMatch[1]);
    }

    // Extract property type
    if (query.includes('villa') || query.includes('فيلا')) params.type = 'Villa';
    if (query.includes('apartment') || query.includes('شقة')) params.type = 'Apartment';

    return params;
  }

  private extractBookingInfo(query: string): any {
    // Simplified extraction - in production, use NLP
    return {};
  }

  private getRelevantListings(params: any): any[] {
    // Simplified - in production, this would query vector database
    // For now, return empty array to minimize tokens
    return [];
  }

  /**
   * Summarize context to minimize tokens
   */
  private summarizeContext(context: RetrievedContext): string {
    if (context.type === 'search' && context.relevantListings?.length) {
      const count = context.relevantListings.length;
      return `Found ${count} properties matching criteria`;
    }
    if (context.type === 'booking') {
      return 'Booking assistance';
    }
    if (context.type === 'listing') {
      return 'Property listing creation';
    }
    return 'General assistance';
  }

  /**
   * Get recent conversation history (limited for token optimization)
   */
  private getRecentHistory(): string {
    const session = this.chatSessionSubject.value;
    const recentMessages = session.messages.slice(-this.MAX_HISTORY_MESSAGES);

    return recentMessages
      .filter(m => m.role !== 'system')
      .map(m => `${m.role === 'user' ? 'U' : 'A'}: ${m.content.substring(0, 100)}`)
      .join('\n');
  }

  /**
   * Session management
   */
  private createNewSession(): ChatSession {
    return {
      id: this.generateId(),
      messages: [],
      language: this.currentLang,
      createdAt: new Date(),
      updatedAt: new Date()
    };
  }

  private createUserMessage(content: string): ChatMessage {
    return {
      id: this.generateId(),
      role: 'user',
      content,
      timestamp: new Date()
    };
  }

  private createErrorMessage(error: any): ChatMessage {
    const lang = this.currentLang;
    const content = lang === 'ar'
      ? 'عذراً، حدث خطأ. يرجى المحاولة مرة أخرى.'
      : 'Sorry, an error occurred. Please try again.';

    return {
      id: this.generateId(),
      role: 'assistant',
      content,
      timestamp: new Date(),
      error: true
    };
  }

  private addMessageToSession(message: ChatMessage): void {
    const currentSession = this.chatSessionSubject.value;
    const updatedSession: ChatSession = {
      ...currentSession,
      messages: [...currentSession.messages, message],
      updatedAt: new Date()
    };
    console.log('📝 Session updated, total messages:', updatedSession.messages.length);
    this.chatSessionSubject.next(updatedSession);
  }

  private cleanAIResponse(text: string): string {
    // Remove any system markers or artifacts
    return text
      .replace(/^(Assistant:|AI:|Bot:)\s*/i, '')
      .replace(/\[.*?\]/g, '')
      .trim();
  }

  /**
   * Get API key from environment or localStorage
   */
  private getAPIKey(): string | null {
    // Check localStorage first (user can add their own key)
    const storedKey = localStorage.getItem('hf_api_key');
    if (storedKey) return storedKey;

    // In production, this would be in environment variables
    // For free tier, users need to provide their own key
    return null;
  }

  /**
   * Storage helpers
   */
  private saveSession(): void {
    const session = this.chatSessionSubject.value;
    localStorage.setItem('chat_session', JSON.stringify(session));
  }

  private loadSession(): void {
    const stored = localStorage.getItem('chat_session');
    if (stored) {
      try {
        const session = JSON.parse(stored);
        this.chatSessionSubject.next(session);
      } catch (e) {
        console.error('Failed to load chat session', e);
      }
    }
  }

  clearSession(): void {
    localStorage.removeItem('chat_session');
    this.chatSessionSubject.next(this.createNewSession());
  }

  openChat(): void {
    this.chatOpenSubject.next(true);
  }

  closeChat(): void {
    this.chatOpenSubject.next(false);
  }

  toggleChat(): void {
    this.chatOpenSubject.next(!this.chatOpenSubject.value);
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}
