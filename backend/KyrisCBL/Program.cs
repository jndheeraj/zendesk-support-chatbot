using KyrisCBL.Config;
using KyrisCBL.Data;
using KyrisCBL.Helpers;
using KyrisCBL.Models;
using KyrisCBL.Pipeline.Agents;
using KyrisCBL.Pipeline.Core;
using KyrisCBL.Pipeline.HumanReview;
using KyrisCBL.Services;
using KyrisCBL.Services.Embedding;
using KyrisCBL.Services.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────────────

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ── CORS ──────────────────────────────────────────────────────────────────────

builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",               // Development
                "https://your-production-domain.com",  // TODO: Replace with your production domain
                "http://your-production-domain.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

// ── Configuration ─────────────────────────────────────────────────────────────

builder.Services.Configure<ChatbotSettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<LoggingSettings>(builder.Configuration.GetSection("Logging"));

// ── Database ──────────────────────────────────────────────────────────────────

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Identity ──────────────────────────────────────────────────────────────────

builder.Services.AddIdentityCore<ChatUser>(o => o.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole<int>>()
    .AddUserManager<UserManager<ChatUser>>()
    .AddSignInManager<SignInManager<ChatUser>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── Authentication ────────────────────────────────────────────────────────────

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath           = "/api/auth/login";
        options.LogoutPath          = "/api/auth/logout";
        options.Cookie.HttpOnly     = true;
        options.Cookie.SameSite     = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Set to Always in production (HTTPS)
        options.SlidingExpiration   = true;
        options.ExpireTimeSpan      = TimeSpan.FromDays(7);
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin        = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; },
            OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; }
        };
    });

// ── Session ───────────────────────────────────────────────────────────────────

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout             = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly         = true;
    options.Cookie.IsEssential      = true;
    options.Cookie.SameSite         = SameSiteMode.Lax;
    options.Cookie.SecurePolicy     = CookieSecurePolicy.None; // Set to Always in production
});

// ── Core Services ─────────────────────────────────────────────────────────────

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
    Microsoft.AspNetCore.Identity.UI.Services.NoOpEmailSender>();

builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddSingleton<ITicketingService, InMemoryTicketingService>();

// ── Embedding / Retrieval ─────────────────────────────────────────────────────

builder.Services.AddHttpClient<RetrievalService>();
builder.Services.AddSingleton<EmbeddingMatcher>();
builder.Services.AddSingleton<FaqEmbeddingGenerator>();
builder.Services.AddSingleton<EscalationLogger>();
builder.Services.AddSingleton<EmbeddingsService>(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChatbotSettings>>().Value;
    return new EmbeddingsService(settings);
});

// ── Agent Pipeline ────────────────────────────────────────────────────────────

builder.Services.AddScoped<IntentClassificationAgent>();
builder.Services.AddScoped<KnowledgeRetrievalAgent>();
builder.Services.AddScoped<ResponseGenerationAgent>();
builder.Services.AddScoped<WorkflowExecutionAgent>();

builder.Services.AddScoped<AgentPipeline>(sp => new AgentPipeline(
    agents: new IAgent[]
    {
        sp.GetRequiredService<IntentClassificationAgent>(),
        sp.GetRequiredService<KnowledgeRetrievalAgent>(),
        sp.GetRequiredService<ResponseGenerationAgent>(),
        sp.GetRequiredService<WorkflowExecutionAgent>()
    },
    logger: sp.GetRequiredService<ILogger<AgentPipeline>>()
));

// ── Request Timeouts / Controllers ────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddRequestTimeouts(options =>
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout           = TimeSpan.FromMinutes(3),
        TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
    });

// ──────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
