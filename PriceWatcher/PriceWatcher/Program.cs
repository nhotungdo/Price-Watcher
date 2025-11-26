using System.Net.Http;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using PriceWatcher.Models;
using PriceWatcher.Options;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;
using PriceWatcher.Services.Scrapers;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PriceWatcherDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBDefault")));

builder.Services.Configure<RecommendationOptions>(builder.Configuration.GetSection("Recommendation"));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

builder.Services.AddHttpClient("telegram")
    .AddPolicyHandler(CreateRetryPolicy());

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = clientFactory.CreateClient("telegram");
    return new TelegramBotClient(options.BotToken ?? string.Empty, httpClient);
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async ctx =>
            {
                var userService = ctx.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var cartSession = ctx.HttpContext.RequestServices.GetRequiredService<ICartSessionService>();
                var googleInfo = new PriceWatcher.Dtos.GoogleUserInfo
                {
                    GoogleId = ctx.Principal?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty,
                    Email = ctx.Principal?.FindFirstValue(System.Security.Claims.ClaimTypes.Email) ?? string.Empty,
                    Name = ctx.Principal?.FindFirstValue(System.Security.Claims.ClaimTypes.Name),
                    AvatarUrl = ctx.Principal?.FindFirstValue("picture")
                };

                var user = await userService.GetOrCreateUserFromGoogleAsync(googleInfo);
                await userService.OnLoginSuccessAsync(user, ctx.HttpContext.Request);
                ctx.Identity?.AddClaim(new Claim("uid", user.UserId.ToString()));
                await cartSession.MergeOnLoginAsync(ctx.HttpContext, user.UserId);
            }
        };
    });

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ILinkProcessor, LinkProcessor>();
builder.Services.AddScoped<IImageSearchService, ImageSearchServiceStub>();
builder.Services.AddSingleton<IImageEmbeddingService, ImageEmbeddingService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IProductSearchService, ProductSearchService>();
builder.Services.AddScoped<ISearchHistoryService, SearchHistoryService>();
builder.Services.AddScoped<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<ISearchStatusService, SearchStatusService>();
builder.Services.AddScoped<ISearchProcessingService, SearchProcessingService>();
builder.Services.AddScoped<IProductScraper, ShopeeScraperStub>();
builder.Services.AddScoped<IProductScraper, LazadaScraperStub>();
builder.Services.AddScoped<IProductScraper, TikiScraperStub>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartSessionService, CartSessionService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register new feature services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IProductComparisonService, ProductComparisonService>();
builder.Services.AddScoped<IPriceAnalyticsService, PriceAnalyticsService>();
builder.Services.AddScoped<ISuggestedProductsService, SuggestedProductsService>();

// Advanced Search & Favorites
builder.Services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IStoreListingService, StoreListingService>();

// Crawler Services
builder.Services.AddHttpClient("tiki", c =>
{
    c.BaseAddress = new Uri("https://tiki.vn");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/json,text/html");
    c.DefaultRequestHeaders.Add("x-api-source", "pc");
});

builder.Services.AddScoped<ITikiCrawler, TikiCrawler>();
builder.Services.AddSingleton<ICrawlQueue, CrawlQueue>();
builder.Services.AddHostedService<TikiCrawlWorker>();

builder.Services.AddHttpClient("shopee", c =>
{
    c.BaseAddress = new Uri("https://shopee.vn");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/json,text/html");
    c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
    c.DefaultRequestHeaders.Add("x-api-source", "pc");
    c.DefaultRequestHeaders.Add("x-shopee-language", "vi");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    CookieContainer = new System.Net.CookieContainer(),
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
}).AddPolicyHandler(CreateRetryPolicy());
builder.Services.AddHttpClient("lazada", c =>
{
    c.BaseAddress = new Uri("https://www.lazada.vn");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    c.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/json");
    c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    CookieContainer = new System.Net.CookieContainer(),
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
}).AddPolicyHandler(CreateRetryPolicy());


builder.Services.AddSingleton(Channel.CreateUnbounded<SearchJob>());
builder.Services.AddSingleton<ISearchJobQueue, SearchJobQueue>();
builder.Services.AddHostedService<SearchProcessingWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PriceWatcherDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
    }

    try
    {
        db.Database.ExecuteSqlRaw(@"IF COL_LENGTH('Users','PasswordHash') IS NULL ALTER TABLE Users ADD PasswordHash VARBINARY(64) NULL;
IF COL_LENGTH('Users','PasswordSalt') IS NULL ALTER TABLE Users ADD PasswordSalt VARBINARY(16) NULL;");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.Carts','U') IS NULL
BEGIN
    CREATE TABLE dbo.Carts (
        CartId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NULL,
        AnonymousId UNIQUEIDENTIFIER NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedAt DATETIME NOT NULL DEFAULT(getutcdate()),
        UpdatedAt DATETIME NOT NULL DEFAULT(getutcdate()),
        ExpiresAt DATETIME NULL
    );
    CREATE INDEX IX_Carts_UserId ON dbo.Carts(UserId);
    CREATE INDEX IX_Carts_AnonymousId ON dbo.Carts(AnonymousId);
END;

IF OBJECT_ID('dbo.CartItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems (
        CartItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CartId INT NOT NULL,
        ProductId INT NULL,
        ProductName NVARCHAR(500) NOT NULL DEFAULT(''),
        PlatformId INT NULL,
        PlatformName NVARCHAR(100) NULL,
        ImageUrl NVARCHAR(1000) NULL,
        ProductUrl NVARCHAR(1000) NULL,
        Price DECIMAL(18,2) NOT NULL,
        OriginalPrice DECIMAL(18,2) NULL,
        Quantity INT NOT NULL DEFAULT(1),
        AddedAt DATETIME NOT NULL DEFAULT(getutcdate()),
        UpdatedAt DATETIME NOT NULL DEFAULT(getutcdate()),
        MetadataJson NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_CartItems_Cart_Product ON dbo.CartItems(CartId, ProductId, PlatformId);
    ALTER TABLE dbo.CartItems WITH CHECK ADD CONSTRAINT FK_CartItems_Carts_CartId FOREIGN KEY(CartId) REFERENCES dbo.Carts(CartId) ON DELETE CASCADE;
END;

");
    }
    catch { }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<PriceWatcher.Middlewares.AnonymousCartMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapRazorPages();

app.Run();

static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => (int)response.StatusCode == 429)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
