# RAG Chatbot Setup Guide

## ⚠️ IMPORTANT: CORS Issue & Current Status

**Current Implementation**: The chatbot is using **rule-based responses** instead of AI API due to CORS (Cross-Origin Resource Sharing) restrictions.

### Why CORS Blocks Direct API Calls

Hugging Face and most AI APIs block direct browser requests for security. You'll see this error:
```
Access to fetch at 'https://api-inference.huggingface.co/...' has been blocked by CORS policy
```

### Solutions (Choose One)

#### ✅ Option 1: Use Rule-Based (Current - Works Now)
- **Status**: Already implemented and working
- **Pros**: No setup needed, works immediately, no API costs
- **Cons**: Simpler responses, no true AI conversation
- **Best for**: Testing, demos, offline functionality

#### Option 2: Backend Proxy (Recommended for Production)
Create an API endpoint in your ASP.NET backend:

```csharp
// Backend/PL/Controllers/ChatController.cs
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string HF_API_KEY = "your_hf_key_here";

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
            "https://api-inference.huggingface.co/models/mistralai/Mixtral-8x7B-Instruct-v0.1");
        
        httpRequest.Headers.Add("Authorization", $"Bearer {HF_API_KEY}");
        httpRequest.Content = JsonContent.Create(new { inputs = request.Prompt });
        
        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        return Ok(content);
    }
}
```

Then update frontend service:
```typescript
// In rag-chat.service.ts
private readonly API_BASE = '/api/chat/message'; // Your backend
private readonly USE_API = true; // Enable API mode
```

#### Option 3: Use Groq (CORS-Friendly Alternative)
Groq API allows browser calls:

1. Get API key: https://console.groq.com
2. Update service:
```typescript
private readonly API_BASE = 'https://api.groq.com/openai/v1/chat/completions';
private readonly USE_API = true;
```

3. Add API key prompt in chat UI

---

## Overview
This document provides step-by-step instructions for setting up the AI-powered RAG (Retrieval-Augmented Generation) chatbot for The Broker platform.

## Architecture
- **Frontend**: Angular 20 standalone component
- **AI Service**: Hugging Face Inference API (Free Tier)
- **Context Retrieval**: Local listings database
- **Token Optimization**: Compact prompts, limited history

## External Setup Required

### 1. Hugging Face Account Setup

#### Step 1: Create Account
1. Go to https://huggingface.co
2. Click "Sign Up" (top right)
3. Register with email or GitHub
4. Verify your email

#### Step 2: Generate API Token
1. Go to Settings: https://huggingface.co/settings/tokens
2. Click "New token"
3. Name it: `airbnb-clone-chat`
4. Select Type: `Read`
5. Click "Generate"
6. **COPY THE TOKEN** - you won't see it again!

#### Step 3: Add Token to Application
1. Open the application in browser
2. Open browser DevTools (F12)
3. Go to Console tab
4. Run: `localStorage.setItem('hf_api_key', 'YOUR_TOKEN_HERE')`
5. Replace `YOUR_TOKEN_HERE` with your copied token
6. Refresh the page

### 2. Alternative Free AI APIs (Optional Fallbacks)

#### Option A: Groq (Faster, Free)
- Website: https://console.groq.com
- Sign up for free account
- Get API key from API Keys section
- Models: `mixtral-8x7b-32768`, `llama2-70b-4096`
- Rate Limit: 14,400 requests/day

**To use Groq instead:**
1. Update `rag-chat.service.ts`:
```typescript
private readonly API_BASE = 'https://api.groq.com/openai/v1';
private readonly MODEL = 'mixtral-8x7b-32768';
```

2. Store key:
```javascript
localStorage.setItem('groq_api_key', 'YOUR_GROQ_KEY');
```

#### Option B: Together AI
- Website: https://api.together.xyz
- Free tier: $25 credit
- Models: Multiple open-source options
- Good for production scaling

#### Option C: OpenRouter (Aggregator)
- Website: https://openrouter.ai
- Aggregates multiple free models
- Easy switching between providers
- Free models available

### 3. Model Options (Ranked by Performance)

1. **Mistral 7B** (Recommended)
   - Model: `mistralai/Mistral-7B-Instruct-v0.2`
   - Best balance of speed/quality
   - Good multilingual support (English/Arabic)

2. **Mixtral 8x7B** (Highest Quality)
   - Model: `mistralai/Mixtral-8x7B-Instruct-v0.1`
   - Best quality responses
   - Slower inference

3. **Llama 2 Chat** (Fallback)
   - Model: `meta-llama/Llama-2-7b-chat-hf`
   - Good quality
   - Fast inference

4. **Falcon** (Lightweight)
   - Model: `tiiuae/falcon-7b-instruct`
   - Fastest
   - Lower quality

### 4. Token Optimization Settings

Current configuration (in `rag-chat.service.ts`):
- `MAX_CONTEXT_TOKENS`: 500 (adjust based on your needs)
- `MAX_HISTORY_MESSAGES`: 6 (keeps conversation context minimal)
- `max_new_tokens`: 150 (response length limit)

**To reduce API costs further:**
```typescript
// Ultra-minimal mode
MAX_CONTEXT_TOKENS = 200;
MAX_HISTORY_MESSAGES = 3;
max_new_tokens = 100;
```

### 5. Rate Limiting

Free tier limits:
- Hugging Face: ~30 requests/minute
- Groq: 14,400 requests/day
- Together AI: Based on credit

**Implemented safeguards:**
- Automatic retry with exponential backoff
- Fallback to rule-based responses
- Request debouncing (300ms)

### 6. Error Handling

The chatbot handles these scenarios:
- ✅ API key missing → Rule-based fallback
- ✅ Rate limit exceeded → Retry with delay
- ✅ Network errors → Show retry button
- ✅ Invalid responses → Fallback model
- ✅ Timeout → Local processing

### 7. Testing the Setup

1. **Test API Connection:**
```javascript
// In browser console
fetch('https://api-inference.huggingface.co/models/mistralai/Mixtral-8x7B-Instruct-v0.1', {
  headers: { 'Authorization': 'Bearer YOUR_TOKEN' },
  method: 'POST',
  body: JSON.stringify({ inputs: "Hello, test message" })
})
.then(r => r.json())
.then(console.log)
```

2. **Test Chatbot:**
- Click the floating broker avatar (bottom-right)
- Type: "Show me properties in Cairo"
- Should get response with search action button

3. **Test Fallback:**
- Remove API key: `localStorage.removeItem('hf_api_key')`
- Send message
- Should get rule-based response

### 8. Production Deployment

#### Environment Variables
Create `.env` file:
```bash
VITE_HF_API_KEY=your_hugging_face_key
VITE_AI_PROVIDER=huggingface  # or groq, together
VITE_AI_MODEL=mistralai/Mixtral-8x7B-Instruct-v0.1
```

#### Backend Proxy (Recommended)
For production, proxy API calls through your backend:
```typescript
// Instead of calling HF directly
private readonly API_BASE = '/api/chat';  // Your backend endpoint
```

Benefits:
- Hides API keys
- Adds rate limiting
- Caching responses
- Analytics tracking

### 9. Cost Optimization

**Free Tier Strategy:**
1. Use Hugging Face for primary
2. Groq as fallback (faster)
3. Rule-based for simple queries
4. Cache common responses

**Estimated Usage:**
- Average query: ~100 tokens
- Response: ~150 tokens
- Total: 250 tokens/chat
- Free tier: ~100,000 tokens/month
- = ~400 conversations/month free

**To scale:**
- Implement response caching
- Add conversation summarization
- Use smaller models for simple queries
- Batch API requests

### 10. Monitoring

Add to your analytics:
```typescript
// Track usage
logEvent('chat_message_sent', { provider: 'huggingface', tokens: 250 });
logEvent('chat_fallback_used', { reason: 'rate_limit' });
```

### 11. Privacy & Compliance

- ✅ No user data sent to AI (only current query)
- ✅ Conversations stored locally only
- ✅ API calls are HTTPS encrypted
- ✅ No personal information in prompts
- ⚠️ Add privacy policy if collecting chat logs

### 12. Troubleshooting

**Issue: "API key invalid"**
- Verify token is correct
- Check it's a READ token
- Regenerate if expired

**Issue: "Model loading" errors**
- First request can take 20-30s (cold start)
- Show loading indicator
- Retry after 30s

**Issue: Responses in wrong language**
- Check `LanguageService.currentLanguage`
- Clear chat and try again
- Verify translation keys loaded

**Issue: High API costs**
- Enable response caching
- Increase debounce time
- Use rule-based for FAQ

### 13. Advanced Features (Future)

- Vector database for better context retrieval
- Fine-tuned model for Egyptian properties
- Multi-turn conversation memory
- Voice input/output
- Property recommendation engine

## Support

For issues:
1. Check browser console for errors
2. Verify API key setup
3. Test with fallback mode
4. Review network tab in DevTools

## API Key Security

**NEVER commit API keys to git!**

Add to `.gitignore`:
```
.env
.env.local
```

Use environment variables in production.

## Quick Start Checklist

- [ ] Create Hugging Face account
- [ ] Generate API token
- [ ] Add token to localStorage
- [ ] Test chat functionality
- [ ] Verify fallback works
- [ ] Add monitoring/analytics
- [ ] Review privacy policy
- [ ] Set up production proxy (optional)

## Resources

- Hugging Face Docs: https://huggingface.co/docs/api-inference
- Groq Documentation: https://console.groq.com/docs
- Model Comparison: https://huggingface.co/spaces/lmsys/chatbot-arena-leaderboard
