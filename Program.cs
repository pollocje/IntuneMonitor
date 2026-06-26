using IntuneMonitor.Hubs;
using IntuneMonitor.Services;
using IntuneMonitor.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGraphService, MockGraphService>();
builder.Services.AddHostedService<EnrollmentMonitorWorker>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapHub<EnrollmentHub>("/enrollmenthub");
app.MapFallbackToPage("/_Host");

app.Run();
