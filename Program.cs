using IntuneMonitor.Data;
using IntuneMonitor.Hubs;
using IntuneMonitor.Services;
using IntuneMonitor.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<TenantOnboardingService>();
builder.Services.AddSingleton<MockGraphService>();
builder.Services.AddSingleton<IGraphService>(sp => sp.GetRequiredService<MockGraphService>());
builder.Services.AddSingleton<GraphServiceFactory>();
builder.Services.AddHttpClient<TeamsNotificationService>();
builder.Services.AddHttpClient<SlackNotificationService>();
builder.Services.AddTransient<EmailNotificationService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddHostedService<EnrollmentMonitorWorker>();
builder.Services.AddHostedService<TrialReminderWorker>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<EnrollmentHub>("/enrollmenthub");

// Stripe webhook — must read raw body before any middleware touches it
app.MapPost("/billing/webhook", async (HttpRequest req, StripeService stripe) =>
{
    var json = await new StreamReader(req.Body).ReadToEndAsync();
    var signature = req.Headers["Stripe-Signature"].ToString();

    try
    {
        await stripe.HandleWebhookAsync(json, signature);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}).AllowAnonymous();

app.MapFallbackToPage("/_Host");

app.Run();
