using System.Threading.RateLimiting;
using Logistics.Gateway;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// 1. CORS — Mevcut yapılandırma korunuyor
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// 2. RATE LIMITING — Fixed Window algoritması
//    • Pencere: 10 saniye
//    • Pencere başına max istek: 100
//    • Tolerans kuyruğu: 5 istek
//    • Fazla istekler: 429 Too Many Requests
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter("fixed", options =>
    {
        options.Window              = TimeSpan.FromSeconds(10);
        options.PermitLimit         = 100;
        options.QueueLimit          = 5;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Sınırı aşan isteklere okunabilir hata mesajı dön
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"error":"Rate limit exceeded. Lütfen daha yavaş istek gönderin.","retryAfter":10}""",
            cancellationToken);
    };
});

// ─────────────────────────────────────────────────────────────────────────────
// 3. POLLY DAYANIKLILIK POLİTİKALARI — HttpClient + Resilience Pipeline
//    • Retry   : 3 deneme, üstel geri çekilme (2s, 4s, 8s)
//    • Circuit Breaker: 30 sn içinde %50'den fazla hata → 30 sn devre açık
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("ResilientForwarder")
    .AddStandardResilienceHandler(resilienceOptions =>
    {
        // Retry politikası
        resilienceOptions.Retry.MaxRetryAttempts = 3;
        resilienceOptions.Retry.Delay            = TimeSpan.FromSeconds(2);
        resilienceOptions.Retry.BackoffType      = DelayBackoffType.Exponential;
        resilienceOptions.Retry.UseJitter        = true; // Sürü etkisini önler

        // Circuit Breaker politikası
        resilienceOptions.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(30);
        resilienceOptions.CircuitBreaker.MinimumThroughput = 5;
        resilienceOptions.CircuitBreaker.FailureRatio      = 0.5;  // %50 hata eşiği
        resilienceOptions.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(30);
    });

// YARP'ın varsayılan HttpClient factory'sini Polly pipeline'lı versiyonuyla değiştir
builder.Services.AddSingleton<IForwarderHttpClientFactory, ResilientHttpClientFactory>();

// ─────────────────────────────────────────────────────────────────────────────
// 4. YARP REVERSE PROXY
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ─────────────────────────────────────────────────────────────────────────────
// UYGULAMA PIPELINE — Middleware sırası kritiktir!
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseRateLimiter();                                          // Rate Limiting middleware
app.MapReverseProxy().RequireRateLimiting("fixed");           // Tüm proxy rotalarına uygula

app.Run();
