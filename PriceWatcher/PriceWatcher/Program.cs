using System.Net.Http;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using PriceWatcher.Models;
using PriceWatcher.Options;
using PriceWatcher.Services;
using PriceWatcher.Services.Interfaces;
using PriceWatcher.Services.Scrapers;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PriceWatcherDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<ISearchHistoryService, SearchHistoryService>();
builder.Services.AddScoped<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<ISearchStatusService, SearchStatusService>();
builder.Services.AddScoped<ISearchProcessingService, SearchProcessingService>();
builder.Services.AddScoped<IProductScraper, ShopeeScraperStub>();
builder.Services.AddScoped<IProductScraper, LazadaScraperStub>();
builder.Services.AddScoped<IProductScraper, TikiScraperStub>();

// Register new feature services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IProductComparisonService, ProductComparisonService>();
builder.Services.AddScoped<IPriceAnalyticsService, PriceAnalyticsService>();

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
    db.Database.EnsureCreated();
    try
    {
        db.Database.ExecuteSqlRaw(@"IF COL_LENGTH('Users','PasswordHash') IS NULL ALTER TABLE Users ADD PasswordHash VARBINARY(64) NULL;
IF COL_LENGTH('Users','PasswordSalt') IS NULL ALTER TABLE Users ADD PasswordSalt VARBINARY(16) NULL;");
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
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();

static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => (int)response.StatusCode == 429)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
