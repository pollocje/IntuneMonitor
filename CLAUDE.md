# IntuneMonitor — Project Context

## What this is
A SaaS product that monitors Microsoft Intune device enrollments in real time. When a laptop is reimaged and enrolled in Intune, IT admins have no way to know when all the required apps have finished installing — they just wait and guess. This app watches the enrollment, shows a live dashboard of app install progress, and sends a Teams/email notification the moment a device is fully ready.

Target customers: IT admins at SMBs, MSPs who reprovision devices regularly.

## Who is building this
Solo project by a final-year CS student doing an IT co-op at a provincial government (Canada). Primary language is C#. Building this as a side project to eventually market and sell as a SaaS.

## Tech stack
- **ASP.NET Core** + **Blazor Server** — backend and frontend (no JavaScript framework)
- **SignalR** — real-time dashboard updates pushed to browser
- **Microsoft Graph API** — pulls Intune device and app install status data
- **Microsoft.Graph NuGet SDK** + **Azure.Identity** — Graph API client
- Teams webhook (Adaptive Cards) for notifications

## Current state
The project is a working skeleton with mock data. Nothing requires a real Intune tenant to run yet.

### What's built
- `Models/` — `DeviceEnrollment` and `AppInstallStatus` models
- `Services/IGraphService` — interface for data access
- `Services/MockGraphService` — simulates enrollment progress (apps install one at a time, new devices spawn when all are ready)
- `Services/GraphService` — real Graph API implementation, ready to use but needs credentials
- `Services/INotificationService` + `TeamsNotificationService` — sends Adaptive Card to Teams webhook when a device is fully enrolled
- `Workers/EnrollmentMonitorWorker` — background service that polls every 10 seconds, pushes to SignalR hub, fires notifications
- `Hubs/EnrollmentHub` — SignalR hub
- `Pages/Dashboard.razor` — live enrollment table with progress bars, summary stat cards, links to detail page
- `Pages/DeviceDetail.razor` — per-device view showing all app statuses and metadata
- `Shared/SummaryCards.razor` — 4 stat cards (Total, Waiting, Installing, Ready)
- `appsettings.json` — config placeholders for Azure AD credentials and Teams webhook URL

### What's NOT built yet
- Real Graph API tested against a live tenant (needs Azure AD app registration with Intune permissions)
- Multi-tenant support (currently single-tenant)
- User authentication / accounts
- Stripe payments / subscription gating
- Landing/marketing page
- Email notifications (only Teams webhook exists)
- Database (no persistence yet — state lives in memory)
- Azure deployment

## How to run locally

```bash
dotnet new blazorserver -n IntuneMonitor --force
dotnet add package Microsoft.AspNetCore.SignalR.Client
dotnet add package Microsoft.Graph
dotnet add package Azure.Identity
```

Delete the generated `Pages/Index.razor` (Dashboard.razor replaces it), then:

```bash
dotnet run
```

Runs on `http://localhost:5000` against mock data by default.

## Switching to real Intune data
1. Register an app in Azure Portal → App registrations
2. Grant API permissions: `DeviceManagementManagedDevices.Read.All` and `DeviceManagementApps.Read.All` (application permissions)
3. Fill in `appsettings.json`:
```json
{
  "AzureAd": {
    "TenantId": "...",
    "ClientId": "...",
    "ClientSecret": "..."
  }
}
```
4. In `Program.cs`, swap `MockGraphService` for `GraphService`

## Enabling Teams notifications
Paste a Teams incoming webhook URL into `appsettings.json`:
```json
{
  "Notifications": {
    "TeamsWebhookUrl": "https://..."
  }
}
```

## Architecture notes
- `MockGraphService` is a singleton that maintains state between polls — this is intentional so the animation works
- `EnrollmentMonitorWorker` tracks which devices it has already notified via an in-memory `HashSet` — this resets on app restart, which is fine for now
- `IGraphService` and `INotificationService` are interfaces so real implementations can be swapped in without touching the worker or pages
- Scoped CSS files (`.razor.css`) are used for component styling — Blazor automatically scopes them

## Next priorities
1. Get a real Microsoft tenant (M365 Business Premium trial or school Azure credits) and test GraphService
2. Add a database (Entity Framework + Postgres) for persistence and multi-tenant support
3. Build the multi-tenant OAuth consent flow so customers can connect their own tenant
4. Stripe integration for subscriptions
5. Deploy to Azure App Service
