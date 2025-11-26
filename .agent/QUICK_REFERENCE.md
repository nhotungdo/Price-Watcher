# Quick Reference Guide - Price Watcher Development

## 📚 Documentation Index

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **PROJECT_SUMMARY.md** | Overview, current status, priorities | Start here! Get the big picture |
| **IMPLEMENTATION_PLAN.md** | Detailed phase-by-phase guide | When implementing features |
| **ARCHITECTURE.md** | Technical architecture & design | Understanding system structure |
| **ROADMAP.md** | Visual timeline & milestones | Planning & tracking progress |
| **QUICK_REFERENCE.md** | This file - quick commands | Daily development tasks |

---

## 🚀 Quick Start Commands

### Development

```powershell
# Navigate to project
cd f:\OJT-Review\PriceWatcher\Price-Watcher\PriceWatcher\PriceWatcher

# Restore packages
dotnet restore

# Build project
dotnet build

# Run application (HTTP)
dotnet run --launch-profile http

# Run with hot reload
dotnet watch run

# Run tests
cd ..\PriceWatcher.Tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Database

```powershell
# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Drop database (careful!)
dotnet ef database drop
```

### Git Workflow

```powershell
# Create feature branch
git checkout -b feature/feature-name

# Check status
git status

# Stage changes
git add .

# Commit
git commit -m "feat: description"

# Push to remote
git push origin feature/feature-name

# Pull latest changes
git pull origin main

# Merge main into feature branch
git merge main
```

---

## 📁 Project Structure Quick Reference

```
PriceWatcher/
├── Controllers/          # API endpoints
│   ├── SearchController.cs
│   ├── ProductController.cs
│   └── Admin/
│
├── Pages/               # Razor Pages (UI)
│   ├── Index.cshtml
│   ├── Search.cshtml
│   ├── Product/
│   └── Admin/
│
├── Services/            # Business logic
│   ├── Interfaces/
│   ├── ProductSearchService.cs
│   ├── PriceAlertService.cs
│   └── Scrapers/
│
├── Models/              # Database entities
│   ├── Product.cs
│   ├── User.cs
│   └── PriceWatcherDbContext.cs
│
├── Dtos/                # Data transfer objects
│   ├── ProductDto.cs
│   └── SearchRequestDto.cs
│
├── wwwroot/             # Static files
│   ├── css/
│   ├── js/
│   └── images/
│
└── Program.cs           # Application startup
```

---

## 🎯 Implementation Checklist

### Phase 1: Enhanced Search (Week 1)

- [ ] Create `Services/AdvancedSearchService.cs`
- [ ] Create `Services/Interfaces/IAdvancedSearchService.cs`
- [ ] Create `Pages/Search.cshtml` and `.cshtml.cs`
- [ ] Create `wwwroot/js/search.js`
- [ ] Create `wwwroot/css/search.css`
- [ ] Add search filters (price, category, platform, rating)
- [ ] Implement sort options
- [ ] Add pagination
- [ ] Update `SearchController.cs` with new endpoint
- [ ] Write unit tests
- [ ] Test UI responsiveness

### Phase 2: Product Detail (Week 2)

- [ ] Create `Pages/Product/Detail.cshtml` and `.cshtml.cs`
- [ ] Create `Services/StoreListingService.cs`
- [ ] Create `Models/StoreListing.cs`
- [ ] Create `wwwroot/js/product-detail.js`
- [ ] Create `wwwroot/css/product-detail.css`
- [ ] Implement price history chart
- [ ] Add store listings table
- [ ] Add related products
- [ ] Create comparison page
- [ ] Write unit tests

### Phase 3: Price Tracking (Week 3)

- [ ] Enhance `Services/PriceAnalyticsService.cs`
- [ ] Create `Services/BackgroundJobs/PriceMonitoringJob.cs`
- [ ] Create `wwwroot/js/price-charts.js`
- [ ] Implement Chart.js integration
- [ ] Add multiple notification channels
- [ ] Create price alert dashboard
- [ ] Add automated monitoring
- [ ] Write unit tests

---

## 🔧 Common Code Snippets

### Creating a New Service

```csharp
// Services/MyService.cs
using PriceWatcher.Models;
using Microsoft.EntityFrameworkCore;

namespace PriceWatcher.Services;

public interface IMyService
{
    Task<Result> DoSomethingAsync(int id);
}

public class MyService : IMyService
{
    private readonly PriceWatcherDbContext _dbContext;
    private readonly ILogger<MyService> _logger;

    public MyService(
        PriceWatcherDbContext dbContext,
        ILogger<MyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> DoSomethingAsync(int id)
    {
        // Implementation
        return new Result();
    }
}
```

### Register Service in Program.cs

```csharp
// Program.cs
builder.Services.AddScoped<IMyService, MyService>();
```

### Creating a New Controller

```csharp
// Controllers/MyController.cs
using Microsoft.AspNetCore.Mvc;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    private readonly IMyService _myService;
    private readonly ILogger<MyController> _logger;

    public MyController(
        IMyService myService,
        ILogger<MyController> logger)
    {
        _myService = myService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _myService.DoSomethingAsync(id);
        return Ok(result);
    }
}
```

### Creating a Razor Page

```csharp
// Pages/MyPage.cshtml.cs
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PriceWatcher.Pages;

public class MyPageModel : PageModel
{
    private readonly IMyService _myService;

    public MyPageModel(IMyService myService)
    {
        _myService = myService;
    }

    public async Task OnGetAsync()
    {
        // Load data
    }
}
```

### Database Query Examples

```csharp
// Simple query
var products = await _dbContext.Products
    .Where(p => p.CategoryId == categoryId)
    .ToListAsync();

// With includes
var products = await _dbContext.Products
    .Include(p => p.Category)
    .Include(p => p.Platform)
    .Where(p => p.Price > minPrice)
    .ToListAsync();

// Projection
var productDtos = await _dbContext.Products
    .Select(p => new ProductDto
    {
        Id = p.ProductId,
        Name = p.ProductName,
        Price = p.CurrentPrice
    })
    .ToListAsync();

// Pagination
var products = await _dbContext.Products
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## 🎨 Frontend Code Snippets

### Fetch API Call

```javascript
// GET request
async function fetchProducts() {
    try {
        const response = await fetch('/api/products');
        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Error:', error);
    }
}

// POST request
async function createProduct(product) {
    try {
        const response = await fetch('/api/products', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(product)
        });
        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Error:', error);
    }
}
```

### Chart.js Example

```javascript
// Price history chart
const ctx = document.getElementById('priceChart').getContext('2d');
const chart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: dates,
        datasets: [{
            label: 'Price',
            data: prices,
            borderColor: '#1a94ff',
            backgroundColor: 'rgba(26, 148, 255, 0.1)',
            tension: 0.4
        }]
    },
    options: {
        responsive: true,
        plugins: {
            legend: {
                display: true
            },
            tooltip: {
                mode: 'index',
                intersect: false
            }
        },
        scales: {
            y: {
                beginAtZero: false,
                ticks: {
                    callback: function(value) {
                        return formatCurrency(value);
                    }
                }
            }
        }
    }
});
```

---

## 🧪 Testing Examples

### Unit Test

```csharp
// PriceWatcher.Tests/Services/MyServiceTests.cs
using Xunit;
using Moq;

namespace PriceWatcher.Tests.Services;

public class MyServiceTests
{
    [Fact]
    public async Task DoSomething_WithValidId_ReturnsResult()
    {
        // Arrange
        var mockDbContext = new Mock<PriceWatcherDbContext>();
        var mockLogger = new Mock<ILogger<MyService>>();
        var service = new MyService(mockDbContext.Object, mockLogger.Object);

        // Act
        var result = await service.DoSomethingAsync(1);

        // Assert
        Assert.NotNull(result);
    }
}
```

---

## 📊 Database Schema Quick Reference

### Core Tables

```sql
-- Products
Products (ProductId, ProductName, CategoryId, PlatformId, CurrentPrice, ImageUrl, CreatedAt)

-- Price History
PriceSnapshots (Id, ProductId, PlatformId, Price, OriginalPrice, CreatedAt)

-- Price Alerts
PriceAlerts (Id, UserId, ProductId, TargetPrice, IsActive, NotificationChannels)

-- Users
Users (Id, Email, Name, GoogleId, CreatedAt)

-- Favorites
Favorites (Id, UserId, ProductId, CollectionName, CreatedAt)

-- Categories
Categories (Id, Name, ParentId, IconClass)

-- Platforms
Platforms (Id, PlatformName, BaseUrl, LogoUrl)
```

---

## 🔍 Debugging Tips

### Enable Detailed Logging

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### View SQL Queries

```csharp
// Enable in Program.cs
builder.Services.AddDbContext<PriceWatcherDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(); // Development only!
    options.EnableDetailedErrors();
});
```

### Browser DevTools

- **F12** - Open DevTools
- **Network Tab** - Monitor API calls
- **Console Tab** - View JavaScript errors
- **Application Tab** - Check localStorage, cookies

---

## 🚨 Common Issues & Solutions

### Issue: Migration fails

```powershell
# Solution: Remove and recreate
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Issue: Port already in use

```powershell
# Solution: Kill process or change port in launchSettings.json
# Find process
netstat -ano | findstr :5000

# Kill process
taskkill /PID <process_id> /F
```

### Issue: NuGet package conflicts

```powershell
# Solution: Clear cache and restore
dotnet nuget locals all --clear
dotnet restore
```

---

## 📞 Resources

### Documentation
- ASP.NET Core: https://docs.microsoft.com/aspnet/core
- Entity Framework: https://docs.microsoft.com/ef/core
- Bootstrap 5: https://getbootstrap.com/docs/5.3
- Chart.js: https://www.chartjs.org/docs

### Tools
- Visual Studio 2022
- VS Code
- SQL Server Management Studio
- Postman (API testing)
- Git

---

## ✅ Daily Checklist

- [ ] Pull latest changes from main
- [ ] Run tests before coding
- [ ] Write tests for new features
- [ ] Commit frequently with meaningful messages
- [ ] Run the app and test manually
- [ ] Check for console errors
- [ ] Review code before pushing
- [ ] Update documentation if needed

---

## 🎯 Current Sprint (Week 1)

**Goal:** Implement Enhanced Search System

**Tasks:**
1. Create AdvancedSearchService
2. Add search filters UI
3. Implement sort options
4. Add pagination
5. Write tests
6. Deploy to staging

**Daily Standup Questions:**
- What did I complete yesterday?
- What will I work on today?
- Any blockers?

---

**Last Updated:** 2025-11-26  
**Version:** 1.0
