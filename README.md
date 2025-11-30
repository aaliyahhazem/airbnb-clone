# Airbnb Clone - Full-Stack Rental Platform

A production-ready, full-stack property rental platform inspired by Airbnb, featuring real-time messaging, booking management, payment integration, and multi-language support.

## 📋 Table of Contents
- [Project Overview](#project-overview)
- [Architecture Overview](#architecture-overview)
- [Technology Stack](#technology-stack)
- [Backend Architecture](#backend-architecture)
- [Frontend Architecture](#frontend-architecture)
- [Security Implementation](#security-implementation)
- [Real-Time Features](#real-time-features)
- [Database Schema](#database-schema)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Deployment](#deployment)

---

## 🎯 Project Overview

This is a comprehensive rental property platform that allows users to:
- Browse and search properties with interactive maps
- Book accommodations with integrated payment processing
- Real-time messaging between guests and hosts
- Receive instant notifications for bookings, payments, and messages
- Manage favorites and reviews
- Admin dashboard for listing approvals and user management
- Multi-language support (English, Arabic)
- Social authentication (Google, Facebook)

### Key Features
- ✅ **Real-time Communication**: SignalR-powered messaging and notifications
- ✅ **Payment Processing**: Stripe integration for secure transactions
- ✅ **Admin Workflow**: Listing approval/rejection system
- ✅ **Geolocation**: Interactive maps with Leaflet.js
- ✅ **Internationalization**: Full i18n support with RTL for Arabic
- ✅ **Social Auth**: Google and Facebook OAuth integration
- ✅ **Email Notifications**: Automated emails for bookings, payments, and reminders
- ✅ **Onboarding**: First-time user walkthrough
- ✅ **Responsive Design**: Bootstrap 5 responsive UI

---

## 🏗️ Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Angular Frontend (SPA)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Components  │  │   Services   │  │  SignalR Hub │      │
│  │  & Guards    │  │   & Stores   │  │   Clients    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           │ HTTP/WebSocket
                           ↓
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Backend (REST API)                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Controllers  │  │ SignalR Hubs │  │    Auth      │      │
│  │   (PL Layer) │  │  Real-time   │  │  JWT + OAuth │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Services    │  │  AutoMapper  │  │  Validators  │      │
│  │  (BLL Layer) │  │   Profiles   │  │    Logic     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Repositories │  │  UnitOfWork  │  │    Entities  │      │
│  │  (DAL Layer) │  │   Pattern    │  │  Domain Rich │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│              SQL Server Database (Relational)                │
│  Users • Listings • Bookings • Payments • Reviews           │
│  Messages • Notifications • Favorites • Amenities           │
└─────────────────────────────────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                   External Services                          │
│  Stripe API • Firebase Auth • Google Maps • SMTP Email      │
└─────────────────────────────────────────────────────────────┘
```

### Architectural Patterns

#### Backend: Three-Layer Architecture (N-Tier)
- **PL (Presentation Layer)**: Controllers, SignalR Hubs, Middleware
- **BLL (Business Logic Layer)**: Services, View Models, Business Rules
- **DAL (Data Access Layer)**: Repositories, Entity Framework, Database Context

#### Frontend: Component-Based Architecture
- **Feature Modules**: Organized by domain (auth, listings, booking, etc.)
- **Core Module**: Shared services, guards, interceptors
- **Shared Module**: Reusable components and utilities

---

## 🛠️ Technology Stack

### Backend Technologies

| Category | Technology | Version | Purpose |
|----------|-----------|---------|---------|
| **Framework** | ASP.NET Core | 8.0 | Web API framework |
| **Language** | C# | 12.0 | Primary language |
| **ORM** | Entity Framework Core | 9.0.10 | Database access |
| **Database** | SQL Server | 2019+ | Relational database |
| **Real-time** | SignalR | 8.0 | WebSocket communication |
| **Authentication** | ASP.NET Identity | 8.0 | User management |
| **Auth Tokens** | JWT Bearer | 8.0 | Token-based auth |
| **Social Auth** | Google OAuth, Facebook OAuth | 8.0 | External authentication |
| **Payments** | Stripe.NET | 50.0.0 | Payment processing |
| **Email** | MailKit + MimeKit | 4.14.x | SMTP email sending |
| **Mapping** | AutoMapper | 15.1.0 | Object-to-object mapping |
| **API Docs** | Swagger/OpenAPI | 6.6.2 | API documentation |
| **Containerization** | Docker | - | Container deployment |

### Frontend Technologies

| Category | Technology | Version | Purpose |
|----------|-----------|---------|---------|
| **Framework** | Angular | 20.3.10 | SPA framework |
| **Language** | TypeScript | 5.9.2 | Type-safe JavaScript |
| **UI Framework** | Bootstrap | 5.3.8 | Responsive CSS |
| **Material Design** | Angular Material | 20.2.11 | UI components |
| **Real-time** | @microsoft/signalr | 10.0.0 | SignalR client |
| **Auth** | Firebase Auth | 11.10.0 | Social authentication |
| **i18n** | @ngx-translate | 15.0.0 | Internationalization |
| **Maps** | Leaflet.js | 1.9.4 | Interactive maps |
| **State Management** | RxJS + Custom Stores | 7.8.0 | Reactive state |
| **HTTP Client** | Angular HttpClient | 20.3.10 | API communication |
| **SSR** | Angular Universal | 20.3.9 | Server-side rendering |

---

## 🔧 Backend Architecture

### Layer Breakdown

#### 1. Presentation Layer (PL)
**Location**: `Backend/PL/`

**Responsibilities**:
- HTTP endpoint handling
- Request/response transformation
- Authentication & Authorization
- SignalR hub management
- Static file serving

**Key Components**:
```
PL/
├── Controllers/          # REST API endpoints
│   ├── AuthController.cs         # Login, Register, Social Auth
│   ├── ListingsController.cs     # CRUD for listings
│   ├── BookingController.cs      # Booking management
│   ├── PaymentController.cs      # Payment operations
│   ├── MessageController.cs      # Messaging endpoints
│   ├── NotificationController.cs # Notification management
│   ├── ReviewController.cs       # Review system
│   ├── FavoriteController.cs     # Favorites/wishlists
│   ├── AdminController.cs        # Admin operations
│   └── MapController.cs          # Geocoding services
├── Hubs/                 # SignalR real-time hubs
│   ├── MessageHub.cs             # Real-time chat
│   └── NotificationHub.cs        # Real-time notifications
├── Helpers/
│   └── Seeder.cs                 # Database seeding
├── Services/
│   └── NotificationPublisher.cs  # SignalR publisher
└── Program.cs            # App configuration & DI
```

#### 2. Business Logic Layer (BLL)
**Location**: `Backend/BLL/`

**Responsibilities**:
- Business rules enforcement
- Data validation
- Service orchestration
- View model mapping
- External service integration

**Key Services**:
```
BLL/Services/Implementation/
├── IdentityService.cs      # User registration, login, JWT generation
├── ListingService.cs       # Listing CRUD, approval workflow
├── BookingService.cs       # Booking creation, validation, availability
├── PaymentService.cs       # Stripe integration, payment confirmation
├── MessageService.cs       # Chat message handling
├── NotificationService.cs  # Notification creation & distribution
├── ReviewService.cs        # Review posting & rating calculations
├── FavoriteService.cs      # Wishlist management
├── EmailService.cs         # Transactional emails
├── MapService.cs           # Geocoding & reverse geocoding
├── AdminService.cs         # Admin dashboard operations
└── TokenService.cs         # JWT token generation/validation
```

**View Models (ModelVM)**:
- **Auth**: LoginVM, RegisterVM, SocialLoginVM
- **Listing**: ListingCreateVM, ListingDetailVM, ListingOverviewVM
- **Booking**: CreateBookingVM, BookingDetailVM
- **Payment**: CreatePaymentVM, PaymentReceiptVM
- **Notification**: CreateNotificationVM, GetNotificationVM
- **Response**: Generic `Response<T>` wrapper for API responses

#### 3. Data Access Layer (DAL)
**Location**: `Backend/DAL/`

**Responsibilities**:
- Database access
- Entity configurations
- Repository pattern
- Unit of Work pattern
- Migrations

**Repository Pattern**:
```csharp
// Generic Repository Interface
public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}

// Unit of Work Pattern
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IListingRepository Listings { get; }
    IBookingRepository Bookings { get; }
    IPaymentRepository Payments { get; }
    IReviewRepository Reviews { get; }
    IMessageRepository Messages { get; }
    INotificationRepository Notifications { get; }
    IFavoriteRepository Favorites { get; }
    
    Task<int> SaveChangesAsync();
    Task ExecuteInTransactionAsync(Func<Task> operation);
}
```

**Entity Configurations**:
- Fluent API configurations for each entity
- Relationships (One-to-Many, Many-to-Many)
- Cascade delete behaviors
- Index definitions
- Value conversions

**Domain Entities (Rich Domain Model)**:
```
DAL/Entities/
├── User.cs          # Identity user + custom fields
├── Listing.cs       # Property listings with approval workflow
├── Booking.cs       # Booking records
├── Payment.cs       # Payment transactions
├── Review.cs        # Guest reviews
├── Message.cs       # Chat messages
├── Notification.cs  # System notifications
├── Favorite.cs      # User wishlists
├── ListingImage.cs  # Property photos
└── Amenity.cs       # Property amenities (keywords)
```

### Domain-Driven Design Patterns

**Rich Domain Model**: Entities encapsulate business logic
```csharp
public class Listing
{
    // Private setters - controlled mutation
    public int Id { get; private set; }
    public string Title { get; private set; }
    public bool IsApproved { get; private set; }
    
    // Factory method
    public static Listing Create(string title, ...) { }
    
    // Business methods
    internal void Approve(string approver, string? notes) { }
    internal void Reject(string rejectedBy, string? notes) { }
    internal bool SoftDelete(string deletedBy) { }
    public void SetPromotion(bool isPromoted, DateTime? endDate) { }
}
```

---

## 🎨 Frontend Architecture

### Project Structure

```
frontend/src/
├── app/
│   ├── core/                    # Singleton services & guards
│   │   ├── guards/
│   │   │   └── auth.guard.ts           # Route protection
│   │   ├── interceptors/
│   │   │   └── auth-interceptor.ts     # JWT token injection
│   │   ├── models/                     # TypeScript interfaces
│   │   └── services/
│   │       ├── auth.service.ts         # Authentication logic
│   │       ├── message-hub.ts          # SignalR messaging client
│   │       ├── message-store.ts        # Message state management
│   │       ├── notification-hub.ts     # SignalR notification client
│   │       ├── notification-store.ts   # Notification state
│   │       ├── language.service.ts     # i18n language switching
│   │       ├── listings/               # Listing services
│   │       ├── favoriteService/        # Favorite services
│   │       ├── map/                    # Map services
│   │       └── api/                    # HTTP API clients
│   ├── features/                # Feature modules
│   │   ├── auth/                       # Login, Register
│   │   ├── home-page/                  # Landing page
│   │   ├── listings/                   # Property listings
│   │   │   ├── list/
│   │   │   ├── detail/
│   │   │   ├── create-edit/
│   │   │   ├── user-listings/
│   │   │   └── admin-listings/
│   │   ├── booking/                    # Booking flow
│   │   ├── payment/                    # Payment processing
│   │   ├── message/                    # Chat interface
│   │   ├── notification/               # Notification center
│   │   ├── favorites/                  # Wishlist
│   │   ├── admin/                      # Admin dashboard
│   │   ├── Map/                        # Map view
│   │   └── onboarding/                 # First-time walkthrough
│   └── shared/                  # Shared components
│       └── components/
│           └── navbar/                 # Navigation bar
├── assets/                      # Static assets
├── environments/                # Environment configs
└── styles/                      # Global styles
```

### State Management Pattern

**Custom Store Services** (inspired by NgRx):
```typescript
@Injectable({ providedIn: 'root' })
export class NotificationStoreService {
  // Private state
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  private unreadCountSubject = new BehaviorSubject<number>(0);
  
  // Public observables
  notifications$ = this.notificationsSubject.asObservable();
  unreadCount$ = this.unreadCountSubject.asObservable();
  
  // Optimistic updates
  markAsRead(id: number): void {
    // Update local state immediately
    const notifications = this.notificationsSubject.value;
    const notification = notifications.find(n => n.id === id);
    if (notification) {
      notification.isRead = true;
      this.notificationsSubject.next([...notifications]);
    }
    // Call API in background
    this.http.put(`/api/notifications/${id}/read`, {}).subscribe();
  }
}
```

### Routing & Navigation

**Route Guards**:
- `AuthGuard`: Protects authenticated routes
- `listingExistsGuard`: Validates listing ID before navigation

**Lazy Loading**: Feature modules loaded on-demand

**Routes**:
```typescript
const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: Home },
  { path: 'auth/login', component: Login },
  { path: 'auth/register', component: Register },
  { path: 'onboarding', component: OnboardingWalkthrough, canActivate: [AuthGuard] },
  { path: 'listings', component: Listings },
  { path: 'listings/:id', component: ListingsDetail },
  { path: 'host', canActivate: [AuthGuard], children: [...] },
  { path: 'booking', component: BookingComponent, canActivate: [AuthGuard] },
  { path: 'payment/:id', component: PaymentComponent, canActivate: [AuthGuard] },
  { path: 'messages', component: ChatWindow, canActivate: [AuthGuard] },
  { path: 'notifications', component: NotificationWindow, canActivate: [AuthGuard] },
  { path: 'favorites', component: FavoritePage, canActivate: [AuthGuard] },
  { path: 'admin', component: AdminDashboard, canActivate: [AuthGuard] },
];
```

---

## 🔐 Security Implementation

### Authentication Flow

```
┌──────────────┐
│  1. Login    │──────┐
│  Request     │      │
└──────────────┘      │
                      ▼
           ┌────────────────────┐
           │ IdentityService    │
           │ Validates Password │
           └────────────────────┘
                      │
                      ▼
           ┌────────────────────┐
           │  TokenService      │
           │  Generates JWT     │
           └────────────────────┘
                      │
                      ▼
           ┌────────────────────┐
           │  Return Token +    │
           │  User Info         │
           └────────────────────┘
                      │
                      ▼
           ┌────────────────────┐
           │  Frontend Stores   │
           │  Token in Cookie   │
           └────────────────────┘
                      │
                      ▼
           ┌────────────────────┐
           │  All Requests      │
           │  Include JWT in    │
           │  Authorization     │
           │  Header            │
           └────────────────────┘
```

### Backend Security Features

#### 1. JWT Authentication
```csharp
// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(20),
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"])
            )
        };
    });
```

**JWT Token Structure**:
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "role": "Guest",
  "exp": 1735689600,
  "iss": "airbnb-clone",
  "aud": "airbnb-clone-users"
}
```

#### 2. ASP.NET Identity Configuration
```csharp
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    // Additional password policies...
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
```

**User Roles**:
- `Admin`: Full system access, listing approvals
- `Host`: Create/manage listings
- `Guest`: Book properties, leave reviews

#### 3. Social Authentication
**Google OAuth**:
```csharp
builder.Services.AddAuthentication()
    .AddGoogle(opts => {
        opts.ClientId = configuration["Authentication:Google:ClientId"];
        opts.ClientSecret = configuration["Authentication:Google:ClientSecret"];
    });
```

**Facebook OAuth**:
```csharp
.AddFacebook(opts => {
    opts.AppId = configuration["Authentication:Facebook:AppId"];
    opts.AppSecret = configuration["Authentication:Facebook:AppSecret"];
});
```

**Firebase Integration (Frontend)**:
```typescript
// Firebase social login
signInWithPopup(this.firebaseAuth, new GoogleAuthProvider())
  .then(result => result.user.getIdToken())
  .then(firebaseToken => {
    // Send to backend for validation & JWT generation
    this.http.post('/api/auth/social-login', { token: firebaseToken })
      .subscribe(response => this.storeToken(response.token));
  });
```

#### 4. CORS Policy
```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});
```

#### 5. Authorization Attributes
```csharp
[Authorize] // Requires authenticated user
public class BookingController : BaseController { }

[Authorize(Roles = "Admin")]
public async Task<IActionResult> ApproveListingAsync(int id) { }
```

### Frontend Security Features

#### 1. HTTP Interceptor (Token Injection)
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();
  if (token) {
    const cloned = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    return next(cloned);
  }
  return next(req);
};
```

#### 2. Route Guards
```typescript
export class AuthGuard implements CanActivate {
  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const hasToken = !!this.auth.getToken();
    if (hasToken) return true;
    
    this.router.navigate(['/auth/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }
}
```

#### 3. Secure Token Storage
- Tokens stored in **HttpOnly cookies** (recommended for production)
- Current implementation uses `localStorage` (client-side accessible)
- Cookie option prevents XSS attacks

#### 4. SignalR Authentication
```typescript
// SignalR connection with JWT
this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5235/notificationsHub?userID=...', {
    accessTokenFactory: () => this.auth.getToken() || ''
  })
  .build();
```

Backend SignalR JWT validation:
```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        
        if (!string.IsNullOrEmpty(accessToken) &&
            path.StartsWithSegments("/notificationsHub"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

### Data Protection

#### 1. SQL Injection Prevention
- **Entity Framework parameterized queries**
- No raw SQL execution with user input

#### 2. XSS Protection
- Angular sanitizes HTML by default
- Content Security Policy headers (recommended addition)

#### 3. Password Security
- **ASP.NET Identity password hashing** (PBKDF2)
- Configurable password complexity requirements

#### 4. Soft Delete Pattern
```csharp
public bool SoftDelete(string deletedBy)
{
    IsDeleted = true;
    DeletedBy = deletedBy;
    DeletedOn = DateTime.UtcNow;
    return true;
}

// Global query filter
builder.Entity<Listing>()
    .HasQueryFilter(l => !l.IsDeleted);
```

---

## 🔄 Real-Time Features

### SignalR Implementation

#### Backend Hubs

**NotificationHub** (Multi-Connection Support):
```csharp
public class NotificationHub : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> 
        UserConnections = new();
    
    public override Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext().Request.Query["userID"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            var dict = UserConnections.GetOrAdd(userId, 
                _ => new ConcurrentDictionary<string, byte>());
            dict[Context.ConnectionId] = 1;
        }
        return base.OnConnectedAsync();
    }
    
    public static List<string> GetConnectionIds(string userId)
    {
        if (UserConnections.TryGetValue(userId, out var dict))
            return dict.Keys.ToList();
        return new List<string>();
    }
}
```

**MessageHub** (Single Connection per User):
```csharp
public class MessageHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();
    
    public static string? GetConnectionId(string userId)
    {
        UserConnections.TryGetValue(userId, out var connectionId);
        return connectionId;
    }
}
```

#### Frontend SignalR Clients

**NotificationHub Client**:
```typescript
export class NotificationHub {
  private hubConnection!: signalR.HubConnection;
  public notificationReceived = new Subject<NotificationDto>();
  public notificationRead = new Subject<{notificationId: number}>();
  
  public startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`http://localhost:5235/notificationsHub?userID=${userID}`, {
        accessTokenFactory: () => this.auth.getToken() || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();
    
    this.hubConnection.on('ReceiveNotification', (notification) => {
      this.notificationReceived.next(notification);
    });
    
    this.hubConnection.on('NotificationRead', (payload) => {
      this.notificationRead.next(payload);
    });
    
    this.hubConnection.start();
  }
}
```

#### Real-Time Event Flow

**Notification Creation**:
```
1. User Action (e.g., booking confirmed)
   ↓
2. NotificationService.CreateAsync()
   ↓
3. Notification saved to database
   ↓
4. INotificationPublisher.PublishAsync()
   ↓
5. NotificationHub.GetConnectionIds(userId)
   ↓
6. Clients.Clients(connectionIds).SendAsync("ReceiveNotification", notification)
   ↓
7. Frontend hub receives event
   ↓
8. NotificationStoreService updates state
   ↓
9. UI auto-updates via Observable subscription
```

**Message Read Notification**:
```
1. User clicks "Mark as Read"
   ↓
2. NotificationStoreService.markAsRead(id)
   ↓
3. Optimistic UI update (immediate)
   ↓
4. API call: PUT /api/notifications/{id}/read
   ↓
5. Backend NotificationController.MarkAsRead()
   ↓
6. SignalR sends "NotificationRead" to all user connections
   ↓
7. Other tabs/devices receive event
   ↓
8. Store updates cross-tab state
```

### Optimistic UI Updates

**Message Store Pattern**:
```typescript
markAsRead(id: number): void {
  // 1. Update local state immediately (optimistic)
  const notifications = this.notificationsSubject.value;
  const notification = notifications.find(n => n.id === id);
  if (notification) {
    notification.isRead = true;
    this.notificationsSubject.next([...notifications]);
    this.reloadUnreadCount();
  }
  
  // 2. Call API in background (fire-and-forget)
  this.http.put(`/api/notifications/${id}/read`, {})
    .subscribe({
      error: (err) => {
        // Rollback on error
        if (notification) notification.isRead = false;
        this.notificationsSubject.next([...notifications]);
      }
    });
}
```

---

## 💾 Database Schema

### Entity Relationship Diagram

```
┌────────────────┐
│     Users      │
│  (Identity)    │
└────────────────┘
        │ 1
        │
        │ N
┌────────────────┐       ┌──────────────┐
│   Listings     │───N───│   Amenities  │
└────────────────┘       └──────────────┘
        │ 1
        │
        │ N
┌────────────────┐
│ ListingImages  │
└────────────────┘
        
┌────────────────┐       ┌──────────────┐
│   Bookings     │───1───│   Payments   │
└────────────────┘       └──────────────┘
        │ 1
        │
        │ 1
┌────────────────┐
│    Reviews     │
└────────────────┘

┌────────────────┐       ┌──────────────┐
│    Messages    │       │ Notifications│
└────────────────┘       └──────────────┘

┌────────────────┐
│   Favorites    │
└────────────────┘
```

### Core Tables

#### Users
```sql
- Id (GUID, PK)
- FullName (nvarchar)
- Email (nvarchar, unique)
- Role (int: Admin=1, Host=2, Guest=3)
- ProfileImg (nvarchar, nullable)
- FirebaseUid (nvarchar, nullable)
- IsActive (bit)
- IsFirstLogin (bit)
- DateCreated (datetime2)
+ Identity fields (PasswordHash, etc.)
```

#### Listings
```sql
- Id (int, PK)
- Title (nvarchar)
- Description (nvarchar)
- PricePerNight (decimal)
- Location (nvarchar)
- Latitude, Longitude (float)
- MaxGuests (int)
- Destination, Type (nvarchar)
- NumberOfRooms, NumberOfBathrooms (int)
- IsPromoted (bit)
- PromotionEndDate (datetime2, nullable)
- IsReviewed, IsApproved (bit)
- SubmittedForReviewAt, ReviewedAt (datetime2)
- ReviewNotes, ReviewedBy (nvarchar, nullable)
- UserId (GUID, FK -> Users)
- MainImageId (int, FK -> ListingImages, nullable)
- IsDeleted (bit)
- CreatedBy, UpdatedBy, DeletedBy (nvarchar)
- CreatedAt, UpdatedOn, DeletedOn (datetime2)
```

#### Bookings
```sql
- Id (int, PK)
- ListingId (int, FK -> Listings)
- GuestId (GUID, FK -> Users)
- CheckInDate, CheckOutDate (datetime2)
- TotalPrice (decimal)
- PaymentStatus (int: Pending, Paid, Failed, Refunded)
- BookingStatus (int: Pending, Active, Completed, Cancelled)
- CreatedAt (datetime2)
```

#### Payments
```sql
- Id (int, PK)
- BookingId (int, FK -> Bookings)
- Amount (decimal)
- PaymentMethod (nvarchar: card, paypal, etc.)
- TransactionId (nvarchar)
- Status (int: Pending, Success, Failed)
- PaidAt (datetime2)
```

#### Notifications
```sql
- Id (int, PK)
- UserId (GUID, FK -> Users)
- Title (nvarchar)
- Body (nvarchar)
- Type (int: System, Booking, Payment, Message)
- ActionUrl, ActionLabel (nvarchar, nullable)
- IsRead (bit)
- CreatedAt (datetime2)
```

#### Messages
```sql
- Id (int, PK)
- SenderId (GUID, FK -> Users)
- ReceiverId (GUID, FK -> Users)
- Content (nvarchar)
- SentAt (datetime2)
- IsRead (bit)
```

#### Favorites
```sql
- Id (int, PK)
- UserId (GUID, FK -> Users)
- ListingId (int, FK -> Listings)
- CreatedAt (datetime2)
```

### Indexes & Performance

**Recommended Indexes**:
```sql
-- Listings
CREATE INDEX IX_Listings_UserId ON Listings(UserId);
CREATE INDEX IX_Listings_IsApproved_IsDeleted ON Listings(IsApproved, IsDeleted);
CREATE INDEX IX_Listings_Destination_Type ON Listings(Destination, Type);

-- Bookings
CREATE INDEX IX_Bookings_GuestId ON Bookings(GuestId);
CREATE INDEX IX_Bookings_ListingId ON Bookings(ListingId);
CREATE INDEX IX_Bookings_CheckInDate_CheckOutDate ON Bookings(CheckInDate, CheckOutDate);

-- Notifications
CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);

-- Messages
CREATE INDEX IX_Messages_ReceiverId_IsRead ON Messages(ReceiverId, IsRead);
```

### Global Query Filters

```csharp
// Soft delete filter
builder.Entity<Listing>()
    .HasQueryFilter(l => !l.IsDeleted);

builder.Entity<ListingImage>()
    .HasQueryFilter(li => !li.IsDeleted);
```

---

## 🚀 Getting Started

### Prerequisites

- **Backend**:
  - .NET 8.0 SDK
  - SQL Server 2019+ (or LocalDB)
  - Visual Studio 2022 / VS Code / Rider

- **Frontend**:
  - Node.js 20.x+
  - npm 10.x+
  - Angular CLI 20.x

### Backend Setup

1. **Clone Repository**
```powershell
git clone https://github.com/Abdelkarimo/airbnb-clone.git
cd airbnb-clone/Backend
```

2. **Configure Database**

Edit `PL/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AirbnbCloneDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

3. **Configure JWT Secret**
```json
{
  "Jwt": {
    "Key": "CHANGE_THIS_TO_A_STRONG_SECRET_KEY_AtLeast32Chars",
    "Issuer": "airbnb-clone",
    "Audience": "airbnb-clone-users",
    "ExpireMinutes": 1440
  }
}
```

4. **Configure Stripe (Optional)**
```json
{
  "Stripe": {
    "SecretKey": "sk_test_YOUR_KEY",
    "PublishableKey": "pk_test_YOUR_KEY",
    "WebhookSecret": "whsec_YOUR_SECRET"
  }
}
```

5. **Configure Email (Optional)**
```json
{
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-app-password",
    "FromEmail": "your-email@gmail.com"
  }
}
```

6. **Run Migrations**
```powershell
cd PL
dotnet ef database update
```

7. **Run Backend**
```powershell
dotnet run
```

Backend will start on `http://localhost:5235`

**API Documentation**: `http://localhost:5235/swagger`

### Frontend Setup

1. **Navigate to Frontend**
```powershell
cd ../frontend
```

2. **Install Dependencies**
```powershell
npm install
```

3. **Configure Environment**

Edit `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5235/api',
  firebaseConfig: {
    apiKey: "YOUR_FIREBASE_API_KEY",
    authDomain: "YOUR_PROJECT.firebaseapp.com",
    projectId: "YOUR_PROJECT_ID",
    // ... other Firebase config
  }
};
```

4. **Start Development Server**
```powershell
npm start
# or
ng serve
```

Frontend will start on `http://localhost:4200`

### Database Seeding

On first run, the backend automatically seeds:
- **Admin accounts**: `admin1@airbnbclone.com` / `Admin@123`
- **Test users**: `user1@gmail.com` / `user123`
- **Sample listings**: 20 properties
- **Sample bookings**: 5 reservations
- **Sample messages**: 20 chat messages
- **Welcome notifications**: For all users

### Default Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin1@airbnbclone.com | Admin@123 |
| Admin | admin2@airbnbclone.com | Admin@123 |
| User | user1@gmail.com | user123 |
| User | user2@gmail.com | user123 |

---

## 📡 API Documentation

### Authentication Endpoints

#### POST /api/auth/register
Register a new user.

**Request**:
```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "password123",
  "role": 3
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "guid",
      "fullName": "John Doe",
      "email": "john@example.com",
      "role": "Guest"
    }
  }
}
```

#### POST /api/auth/login
Authenticate user.

**Request**:
```json
{
  "email": "john@example.com",
  "password": "password123"
}
```

#### POST /api/auth/social-login
Login with Firebase social token.

**Request**:
```json
{
  "token": "firebase-id-token"
}
```

### Listing Endpoints

#### GET /api/listings
Get all listings (with filtering).

**Query Parameters**:
- `destination` (string, optional)
- `type` (string, optional)
- `minPrice` (decimal, optional)
- `maxPrice` (decimal, optional)

#### GET /api/listings/{id}
Get listing details.

#### POST /api/listings
Create a new listing (Host only).

**Request**:
```json
{
  "title": "Cozy Apartment",
  "description": "Beautiful 2BR in downtown",
  "pricePerNight": 120.00,
  "location": "New York, NY",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "maxGuests": 4,
  "destination": "New York",
  "type": "Apartment",
  "numberOfRooms": 2,
  "numberOfBathrooms": 1,
  "keywordNames": ["wifi", "parking", "kitchen"]
}
```

#### PUT /api/listings/{id}
Update listing (Host/Admin).

#### DELETE /api/listings/{id}
Soft delete listing.

#### POST /api/admin/listings/{id}/approve
Approve listing (Admin only).

#### POST /api/admin/listings/{id}/reject
Reject listing (Admin only).

### Booking Endpoints

#### POST /api/bookings
Create booking.

**Request**:
```json
{
  "listingId": 1,
  "checkInDate": "2024-12-20",
  "checkOutDate": "2024-12-25",
  "totalPrice": 600.00
}
```

#### GET /api/bookings/user
Get user's bookings.

#### PUT /api/bookings/{id}/cancel
Cancel booking.

### Payment Endpoints

#### POST /api/payments/initiate
Initiate payment.

**Request**:
```json
{
  "bookingId": 1,
  "amount": 600.00,
  "paymentMethod": "card"
}
```

#### POST /api/payments/confirm
Confirm payment.

#### POST /api/payments/webhook
Stripe webhook handler.

### Notification Endpoints

#### GET /api/notifications
Get user notifications.

#### GET /api/notifications/unread-count
Get unread count.

#### PUT /api/notifications/{id}/read
Mark notification as read.

#### PUT /api/notifications/mark-all-read
Mark all as read.

### Message Endpoints

#### GET /api/messages/conversations
Get all conversations.

#### GET /api/messages/conversation/{userId}
Get messages with specific user.

#### POST /api/messages
Send message.

**Request**:
```json
{
  "receiverId": "user-guid",
  "content": "Hello!"
}
```

#### PUT /api/messages/{id}/read
Mark message as read.

---

## 🌐 Internationalization (i18n)

### Supported Languages
- **English** (en-US) - Default
- **Arabic** (ar) - RTL support

### Implementation

**Backend**:
```csharp
builder.Services.AddLocalization(options => 
    options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en-US", "ar" };
    options.SetDefaultCulture("en-US")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
    
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider { QueryStringKey = "lang" },
        new CookieRequestCultureProvider { CookieName = "app_language" },
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});
```

**Frontend**:
```typescript
// Language service
export class LanguageService {
  setLanguage(lang: 'en' | 'ar') {
    this.translate.use(lang);
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }
}
```

**Usage in Templates**:
```html
<h1>{{ 'home.welcome' | translate }}</h1>
<button>{{ 'common.submit' | translate }}</button>
```

---

## 🐳 Deployment

### Docker Deployment

**Dockerfile** (Backend):
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PL/PL.csproj", "PL/"]
RUN dotnet restore "PL/PL.csproj"
COPY . .
RUN dotnet build "PL/PL.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PL/PL.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PL.dll"]
```

**Build & Run**:
```powershell
# Build image
docker build -t airbnb-clone-backend -f Backend/PL/Dockerfile Backend/

# Run container
docker run -d -p 5235:8080 --name airbnb-backend airbnb-clone-backend
```

### Production Considerations

1. **Environment Variables**:
   - Move secrets to environment variables
   - Use Azure Key Vault / AWS Secrets Manager

2. **HTTPS Configuration**:
   - Enable HTTPS redirection
   - Configure SSL certificates

3. **Database**:
   - Use managed SQL Server (Azure SQL, AWS RDS)
   - Enable connection pooling
   - Configure backup strategy

4. **Frontend Build**:
```powershell
ng build --configuration production
```

5. **Hosting Options**:
   - **Backend**: Azure App Service, AWS Elastic Beanstalk, Docker
   - **Frontend**: Azure Static Web Apps, Netlify, Vercel
   - **Database**: Azure SQL, AWS RDS, SQL Server on VM

---

## 📊 Performance Optimizations

### Backend

1. **Entity Framework**:
   - `.AsNoTracking()` for read-only queries
   - Eager loading with `.Include()` to prevent N+1 queries
   - Projection with `.Select()` to load only needed fields

2. **Caching** (Recommended Addition):
   - Distributed cache (Redis) for session data
   - Response caching for frequently accessed data

3. **Async/Await**:
   - All database operations are async
   - Non-blocking I/O

### Frontend

1. **Lazy Loading**:
   - Route-based code splitting
   - On-demand module loading

2. **Change Detection**:
   - OnPush strategy for components
   - Immutable data patterns

3. **RxJS Optimizations**:
   - Unsubscribe from observables in `ngOnDestroy`
   - Use `shareReplay()` for shared observables

---

## 🧪 Testing (Recommended Additions)

### Backend Testing
```csharp
// Unit tests with xUnit
[Fact]
public async Task CreateListing_ShouldReturnSuccess()
{
    // Arrange
    var service = new ListingService(mockUow);
    var vm = new ListingCreateVM { Title = "Test" };
    
    // Act
    var result = await service.CreateAsync(vm, userId);
    
    // Assert
    Assert.True(result.Success);
}
```

### Frontend Testing
```typescript
// Karma + Jasmine
describe('AuthService', () => {
  it('should login successfully', () => {
    const service = TestBed.inject(AuthService);
    service.login('test@test.com', 'password').subscribe(result => {
      expect(result.success).toBe(true);
    });
  });
});
```

---

## 📝 Code Quality

### Backend
- **Rich Domain Models**: Encapsulated business logic
- **Repository Pattern**: Abstracted data access
- **Unit of Work**: Transaction management
- **Dependency Injection**: Loose coupling
- **AutoMapper**: DTO mapping

### Frontend
- **Standalone Components**: Modern Angular architecture
- **Reactive Programming**: RxJS observables
- **Type Safety**: TypeScript strict mode
- **Code Formatting**: Prettier configuration
- **Linting**: ESLint (recommended addition)

---

## 🤝 Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open Pull Request

---

## 📄 License

This project is licensed under the MIT License.

---

## 👥 Authors

- **Abdelkarim** - [GitHub](https://github.com/Abdelkarimo)

---

## 🙏 Acknowledgments

- Inspired by Airbnb's platform design
- Built with ASP.NET Core and Angular
- SignalR for real-time features
- Stripe for payment processing
- Leaflet.js for mapping
- Bootstrap for responsive design

---

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Email: airbnbconnect.app@gmail.com

---

**Last Updated**: November 30, 2024
