# IntuneMonitor

**Know the second a device is ready. Stop babysitting Intune.**

When you reimage a laptop and enroll it in Intune, there's no native way to know when all the required apps have finished installing. You're left refreshing the portal manually, guessing, and fielding calls from users asking "is my computer ready yet?"

IntuneMonitor fixes that. It watches your Intune enrollments in real time and notifies you the moment a device is fully set up — every required app installed, no manual checking required.

---

## Features

- **Live enrollment dashboard** — see every device currently enrolling with per-app install progress bars
- **Real-time updates** — dashboard refreshes automatically via SignalR, no page reloads
- **Teams & email notifications** — get alerted the moment a device is ready
- **Failure visibility** — see which apps are stuck or failed immediately
- **Device detail view** — drill into any device to see full app status and enrollment timeline
- **Enrollment history** — track how long enrollments take across your fleet over time
- **Time-to-ready tracking** — know exactly how long each enrollment took

---

## Tech Stack

- [ASP.NET Core](https://dotnet.microsoft.com/) — backend
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — frontend (no JavaScript framework)
- [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) — real-time dashboard updates
- [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview) — Intune device and app data
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) + PostgreSQL — persistence and multi-tenant data

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (or run against mock data without it)
- A Microsoft tenant with Intune (or use built-in mock data for development)

### Run locally

```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet restore
dotnet run
```

Open `http://localhost:5000` — the landing page loads at `/`, the dashboard is at `/dashboard`.

By default the app runs against **animated mock data** so you can explore the full UI without a real Intune tenant or database.

### Set up the database

Install the EF CLI tool if you don't have it:

```bash
dotnet tool install --global dotnet-ef
```

Update the connection string in `appsettings.json`, then run:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Connect to a real Intune tenant

1. Register an app in [Azure Portal](https://portal.azure.com) → App registrations
2. Add these API permissions (application):
   - `DeviceManagementManagedDevices.Read.All`
   - `DeviceManagementApps.Read.All`
3. Fill in `appsettings.json`:

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

4. In `Program.cs`, swap `MockGraphService` for `GraphService`

### Enable Teams notifications

Create an incoming webhook in any Teams channel and paste the URL into `appsettings.json`:

```json
{
  "Notifications": {
    "TeamsWebhookUrl": "https://your-webhook-url"
  }
}
```

---

## Project Structure

```
IntuneMonitor/
├── Data/
│   ├── AppDbContext.cs
│   └── Entities/          # Tenant, AppUser, EnrollmentRecord
├── Hubs/
│   └── EnrollmentHub.cs   # SignalR hub
├── Models/                # DeviceEnrollment, AppInstallStatus
├── Pages/
│   ├── Index.razor        # Landing / marketing page
│   ├── Dashboard.razor    # Live enrollment dashboard (/dashboard)
│   └── DeviceDetail.razor # Per-device detail view (/device/{id})
├── Services/
│   ├── IGraphService.cs
│   ├── MockGraphService.cs  # Animated mock — no tenant needed
│   ├── GraphService.cs      # Real Graph API implementation
│   ├── INotificationService.cs
│   └── TeamsNotificationService.cs
├── Shared/
│   ├── SummaryCards.razor   # Stat cards component
│   └── EmptyLayout.razor    # Layout for public pages
└── Workers/
    └── EnrollmentMonitorWorker.cs  # Background polling + notifications
```

---

## Roadmap

- [x] Live enrollment dashboard with real-time SignalR updates
- [x] Animated mock data for development
- [x] Teams webhook notifications (Adaptive Cards)
- [x] Device detail page
- [x] Summary stat cards
- [x] Database schema (EF Core + PostgreSQL)
- [x] Real Graph API implementation
- [x] Landing / marketing page
- [ ] User authentication (sign up / login)
- [ ] Microsoft OAuth tenant connect flow
- [ ] Multi-tenant worker
- [ ] Email notifications
- [ ] Stripe subscription payments
- [ ] Azure deployment
