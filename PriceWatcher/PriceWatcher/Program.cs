using System.Net.Http;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
    });

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILinkProcessor, LinkProcessor>();
builder.Services.AddScoped<IImageSearchService, ImageSearchServiceStub>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ISearchHistoryService, SearchHistoryService>();
builder.Services.AddScoped<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<ISearchStatusService, SearchStatusService>();
builder.Services.AddScoped<ISearchProcessingService, SearchProcessingService>();
builder.Services.AddScoped<IProductScraper, ShopeeScraperStub>();
builder.Services.AddScoped<IProductScraper, LazadaScraperStub>();
builder.Services.AddScoped<IProductScraper, TikiScraperStub>();

builder.Services.AddSingleton(Channel.CreateUnbounded<SearchJob>());
builder.Services.AddSingleton<ISearchJobQueue, SearchJobQueue>();
builder.Services.AddHostedService<SearchProcessingWorker>();

var app = builder.Build();

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

app.UseHttpsRedirection();
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
