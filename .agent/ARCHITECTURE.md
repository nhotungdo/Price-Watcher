# Price Watcher - System Architecture

## System Overview

Price Watcher is a comprehensive e-commerce price comparison platform built on ASP.NET Core 8 with a microservices-inspired architecture.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Presentation Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Razor Pages  │  │   Web API    │  │  Static Web  │          │
│  │   (MVC)      │  │ Controllers  │  │   (wwwroot)  │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                      Application Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Services   │  │  Background  │  │    DTOs      │          │
│  │   (Business  │  │    Jobs      │  │  (Transfer   │          │
│  │    Logic)    │  │  (Workers)   │  │   Objects)   │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Models     │  │  Interfaces  │  │   Business   │          │
│  │  (Entities)  │  │  (Contracts) │  │    Rules     │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Database    │  │   External   │  │    Cache     │          │
│  │  (EF Core)   │  │   Services   │  │   (Redis)    │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
```

## Technology Stack

### Backend
- **Framework:** ASP.NET Core 8.0
- **Language:** C# 12
- **ORM:** Entity Framework Core 8.0
- **Database:** SQL Server 2022
- **Authentication:** ASP.NET Core Identity + Google OAuth
- **Caching:** In-Memory Cache / Redis (optional)
- **Background Jobs:** Hosted Services / Hangfire (optional)

### Frontend
- **Template Engine:** Razor Pages
- **CSS Framework:** Bootstrap 5.3
- **JavaScript:** Vanilla JS + jQuery
- **Charts:** Chart.js
- **Icons:** Font Awesome / Bootstrap Icons

### External Services
- **Telegram Bot API:** Notifications
- **Image Processing:** System.Drawing / ImageSharp
- **HTTP Client:** Polly (retry policies)

## Project Structure

```
PriceWatcher/
├── Controllers/              # API Controllers
│   ├── AuthController.cs
│   ├── SearchController.cs
│   ├── ProductController.cs
│   ├── CartController.cs
│   ├── PriceAlertsController.cs
│   └── Admin/
│       ├── DashboardController.cs
│       └── ManagementController.cs
│
├── Pages/                    # Razor Pages
│   ├── Index.cshtml
│   ├── Search.cshtml
│   ├── Product/
│   │   ├── Detail.cshtml
│   │   └── Compare.cshtml
│   ├── Account/
│   │   ├── Profile.cshtml
│   │   ├── Favorites.cshtml
│   │   └── Dashboard.cshtml
│   └── Admin/
│       ├── Dashboard.cshtml
│       ├── Products.cshtml
│       └── Analytics.cshtml
│
├── Models/                   # Domain Entities
│   ├── User.cs
│   ├── Product.cs
│   ├── Platform.cs
│   ├── Category.cs
│   ├── PriceSnapshot.cs
│   ├── PriceAlert.cs
│   ├── Review.cs
│   ├── Store.cs
│   ├── Favorite.cs
│   ├── DiscountCode.cs
│   └── PriceWatcherDbContext.cs
│
├── Services/                 # Business Logic
│   ├── Interfaces/
│   │   ├── ISearchService.cs
│   │   ├── IProductService.cs
│   │   ├── IPriceAlertService.cs
│   │   └── ...
│   ├── SearchService.cs
│   ├── ProductService.cs
│   ├── PriceAlertService.cs
│   ├── RecommendationService.cs
│   ├── CartService.cs
│   ├── UserService.cs
│   ├── Scrapers/
│   │   ├── TikiScraper.cs
│   │   ├── ShopeeScraper.cs
│   │   └── LazadaScraper.cs
│   └── BackgroundJobs/
│       ├── PriceCrawlerJob.cs
│       └── PriceMonitoringJob.cs
│
├── Dtos/                     # Data Transfer Objects
│   ├── ProductDto.cs
│   ├── SearchRequestDto.cs
│   ├── PriceAlertDto.cs
│   └── ...
│
├── Middlewares/              # Custom Middleware
│   ├── ErrorHandlingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── PermissionMiddleware.cs
│
├── wwwroot/                  # Static Files
│   ├── css/
│   │   ├── site.css
│   │   ├── layout.css
│   │   ├── product.css
│   │   └── admin.css
│   ├── js/
│   │   ├── site.js
│   │   ├── search.js
│   │   ├── product.js
│   │   ├── cart.js
│   │   └── charts.js
│   └── images/
│
├── Database/                 # Database Scripts
│   ├── Migrations/
│   └── Seeds/
│
├── Options/                  # Configuration Options
│   ├── GoogleAuthOptions.cs
│   ├── TelegramOptions.cs
│   └── RecommendationOptions.cs
│
├── Program.cs               # Application Entry Point
├── appsettings.json         # Configuration
└── PriceWatcher.csproj      # Project File
```

## Core Components

### 1. Search System

**Components:**
- `SearchController` - Handles search requests
- `AdvancedSearchService` - Implements search logic
- `LinkProcessor` - Processes product URLs
- `ImageSearchService` - Image-based search

**Flow:**
```
User Input → SearchController → AdvancedSearchService
    ↓
LinkProcessor/ImageSearchService
    ↓
Scrapers (Tiki/Shopee/Lazada)
    ↓
Database (Products, PriceSnapshots)
    ↓
RecommendationService
    ↓
Search Results
```

### 2. Price Tracking System

**Components:**
- `PriceAlertService` - Manages price alerts
- `PriceCrawlerJob` - Background job for price updates
- `PriceMonitoringJob` - Monitors price changes
- `NotificationService` - Sends alerts

**Flow:**
```
User Sets Alert → PriceAlertService → Database
    ↓
PriceCrawlerJob (Scheduled)
    ↓
Fetch Latest Prices
    ↓
PriceMonitoringJob
    ↓
Compare with Alert Thresholds
    ↓
NotificationService (Email/Telegram/Push)
```

### 3. Product Comparison

**Components:**
- `CompareService` - Product comparison logic
- `ProductService` - Product data management
- `StoreListingService` - Store information

**Flow:**
```
User Selects Products → CompareService
    ↓
Fetch Product Details + Prices
    ↓
Normalize Data
    ↓
Generate Comparison Matrix
    ↓
Display Results
```

### 4. Recommendation Engine

**Components:**
- `RecommendationService` - Core recommendation logic
- `UserBehaviorAnalyzer` - Analyzes user patterns
- `ProductRecommendationEngine` - AI-based suggestions

**Algorithm:**
```
Input: User ID, Context
    ↓
Fetch User History (searches, views, favorites)
    ↓
Content-Based Filtering
    - Similar products by category
    - Similar products by price range
    - Similar products by specifications
    ↓
Collaborative Filtering
    - Users with similar behavior
    - Products they liked
    ↓
Hybrid Ranking
    - Combine scores
    - Apply business rules
    - Sort by relevance
    ↓
Output: Recommended Products
```

### 5. Admin System

**Components:**
- `AdminDashboardService` - Dashboard data
- `AdminProductService` - Product management
- `PermissionService` - Access control
- `RevenueAnalyticsService` - Revenue tracking

**Features:**
- CRUD operations for all entities
- Bulk operations
- Analytics and reporting
- User management
- System configuration

## Database Schema

### Core Tables

```sql
-- Users & Authentication
Users (Id, Email, Name, GoogleId, CreatedAt, LastLogin)
UserRoles (UserId, RoleId)
AdminRoles (Id, Name, Description)
Permissions (Id, Name, Resource, Action)
RolePermissions (RoleId, PermissionId)

-- Products & Catalog
Products (Id, Name, Description, CategoryId, ImageUrl, CreatedAt)
Categories (Id, Name, ParentId, IconClass)
Platforms (Id, Name, BaseUrl, LogoUrl)
ProductMappings (ProductId, PlatformId, ExternalId, Url)

-- Pricing
PriceSnapshots (Id, ProductId, PlatformId, Price, OriginalPrice, CreatedAt)
StoreListings (Id, ProductId, PlatformId, StoreName, Price, Stock)
PriceAlerts (Id, UserId, ProductId, TargetPrice, IsActive, TriggeredAt)

-- User Data
Favorites (Id, UserId, ProductId, CollectionName, CreatedAt)
SearchHistory (Id, UserId, Query, ResultCount, CreatedAt)
Carts (Id, UserId, CreatedAt, UpdatedAt)
CartItems (Id, CartId, ProductId, Quantity, Price)

-- Reviews & Ratings
Reviews (Id, ProductId, UserId, Rating, Comment, IsVerified, CreatedAt)
Stores (Id, Name, PlatformId, Rating, IsVerified, TotalSales)

-- Discounts & Promotions
DiscountCodes (Id, Code, PlatformId, DiscountValue, ExpiresAt, IsActive)

-- System
CrawlJobs (Id, Url, Status, ProductId, CreatedAt, CompletedAt)
SystemLogs (Id, Level, Message, Exception, CreatedAt)
AffiliateLinks (Id, ProductId, PlatformId, Url, ClickCount, Revenue)
```

### Indexes

```sql
-- Performance Indexes
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_PriceSnapshots_ProductId_CreatedAt ON PriceSnapshots(ProductId, CreatedAt DESC);
CREATE INDEX IX_PriceAlerts_UserId_IsActive ON PriceAlerts(UserId, IsActive);
CREATE INDEX IX_SearchHistory_UserId_CreatedAt ON SearchHistory(UserId, CreatedAt DESC);
CREATE INDEX IX_Favorites_UserId ON Favorites(UserId);
```

## API Endpoints

### Public API

```
# Search
GET    /api/search?q={query}&page={page}&limit={limit}
POST   /api/search/advanced
POST   /api/search/image

# Products
GET    /api/products
GET    /api/products/{id}
GET    /api/products/{id}/prices
GET    /api/products/{id}/stores
GET    /api/products/compare?ids={id1,id2,id3}

# Categories
GET    /api/categories
GET    /api/categories/{id}/products

# Price Alerts
POST   /api/alerts
GET    /api/alerts
PUT    /api/alerts/{id}
DELETE /api/alerts/{id}

# User
GET    /api/user/profile
PUT    /api/user/profile
GET    /api/user/favorites
POST   /api/user/favorites
DELETE /api/user/favorites/{id}
GET    /api/user/history

# Cart
GET    /api/cart
POST   /api/cart/items
PUT    /api/cart/items/{id}
DELETE /api/cart/items/{id}
```

### Admin API

```
# Dashboard
GET    /api/admin/dashboard/stats
GET    /api/admin/dashboard/charts

# Products
GET    /api/admin/products
POST   /api/admin/products
PUT    /api/admin/products/{id}
DELETE /api/admin/products/{id}
POST   /api/admin/products/bulk-import

# Crawl Management
GET    /api/admin/crawl/jobs
POST   /api/admin/crawl/trigger
GET    /api/admin/crawl/logs

# Analytics
GET    /api/admin/analytics/revenue
GET    /api/admin/analytics/users
GET    /api/admin/analytics/products
```

## Security

### Authentication
- Google OAuth 2.0
- JWT tokens for API
- Session-based for web pages
- Refresh token rotation

### Authorization
- Role-based access control (RBAC)
- Permission-based authorization
- Resource-level permissions
- Admin vs. User roles

### Data Protection
- HTTPS only
- CSRF tokens
- XSS prevention
- SQL injection prevention (parameterized queries)
- Input validation
- Output encoding

### Rate Limiting
```csharp
// API rate limits
- Anonymous: 100 requests/hour
- Authenticated: 1000 requests/hour
- Admin: Unlimited
```

## Performance Optimization

### Caching Strategy

```csharp
// Cache Layers
1. In-Memory Cache (Fast, volatile)
   - Product details (5 minutes)
   - Category tree (1 hour)
   - User sessions

2. Distributed Cache (Redis)
   - Search results (15 minutes)
   - Price snapshots (1 hour)
   - Popular products (30 minutes)

3. Database Query Cache
   - EF Core query caching
   - Compiled queries
```

### Database Optimization
- Indexed columns for frequent queries
- Denormalized data for read-heavy operations
- Partitioning for large tables (PriceSnapshots)
- Connection pooling
- Async queries

### Frontend Optimization
- Lazy loading images
- Code splitting
- Minification and bundling
- CDN for static assets
- Browser caching headers
- Gzip compression

## Scalability

### Horizontal Scaling
- Stateless application design
- Load balancer (Azure Load Balancer / AWS ELB)
- Multiple app instances
- Shared session storage (Redis)

### Vertical Scaling
- Database scaling (read replicas)
- Caching layer
- Background job workers

### Microservices (Future)
```
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   Search    │  │   Product   │  │    Price    │
│   Service   │  │   Service   │  │   Service   │
└─────────────┘  └─────────────┘  └─────────────┘
       │                │                │
       └────────────────┴────────────────┘
                        │
                  ┌─────────────┐
                  │  API Gateway│
                  └─────────────┘
```

## Monitoring & Logging

### Application Monitoring
- Application Insights / New Relic
- Performance metrics
- Error tracking
- User analytics

### Logging
```csharp
// Log Levels
- Trace: Detailed diagnostic information
- Debug: Development debugging
- Information: General application flow
- Warning: Abnormal or unexpected events
- Error: Errors and exceptions
- Critical: Critical failures
```

### Health Checks
```
GET /health
GET /health/ready
GET /health/live
```

## Deployment

### Environments
1. **Development** - Local development
2. **Staging** - Pre-production testing
3. **Production** - Live environment

### CI/CD Pipeline
```
Code Push → GitHub
    ↓
GitHub Actions Trigger
    ↓
Build & Test
    ↓
Docker Image Build
    ↓
Push to Container Registry
    ↓
Deploy to Staging
    ↓
Automated Tests
    ↓
Manual Approval
    ↓
Deploy to Production
    ↓
Health Check
```

### Infrastructure as Code
```yaml
# Azure Resources
- App Service Plan (Standard S1)
- App Service (Web App)
- SQL Database (Standard S2)
- Redis Cache (Basic C1)
- Storage Account (Blob)
- Application Insights
- Key Vault (Secrets)
```

## Disaster Recovery

### Backup Strategy
- **Database:** Daily automated backups (30-day retention)
- **Files:** Blob storage with geo-redundancy
- **Configuration:** Version controlled in Git

### Recovery Plan
1. Database restore from backup
2. Application redeployment
3. DNS failover (if multi-region)
4. Data validation
5. Service restoration

## Future Enhancements

### Phase 2 (Q2 2026)
- Machine learning price prediction
- Real-time price updates (WebSockets)
- Mobile apps (React Native)
- Advanced analytics dashboard

### Phase 3 (Q3 2026)
- Microservices architecture
- Multi-region deployment
- GraphQL API
- Blockchain-based reviews

### Phase 4 (Q4 2026)
- AI chatbot support
- Voice search
- AR product visualization
- Social commerce features

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-26  
**Maintained By:** Development Team
