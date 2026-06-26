# IntuneMonitor

**Know the second a device is ready. Stop babysitting Intune.**

When you reimage a laptop and enroll it in Intune, there's no native way to know when all the required apps have finished installing. You're left refreshing the portal manually, guessing, and fielding calls from users asking "is my computer ready yet?"

IntuneMonitor fixes that. It watches your Intune enrollments in real time and notifies you the moment a device is fully set up — every required app installed, no manual checking required.

---

## Features

- **Live enrollment dashboard** — see every device currently enrolling, with per-app install progress
- **Real-time updates** — dashboard refreshes automatically via SignalR, no page reloads
- **Instant notifications** — get a Teams or email alert the moment a device is ready
- **Failure visibility** — see which apps are stuck or failed so you can act immediately instead of finding out an hour later
- **Time-to-ready tracking** — know how long enrollments are actually taking across your fleet

---

## Tech Stack

- [ASP.NET Core](https://dotnet.microsoft.com/) — backend
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — frontend (no JavaScript framework)
- [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) — real-time dashboard updates
- [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview) — Intune device and app data

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A Microsoft tenant with Intune (or run against mock data for development)

### Run locally

```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet restore
dotnet run
```

Open `http://localhost:5000` in your browser.

By default the app runs against **mock data** so you can explore the dashboard without a real Intune tenant.

### Connect to a real tenant

1. Register an app in [Azure Portal](https://portal.azure.com) → App registrations
2. Add these API permissions (application):
   - `DeviceManagementManagedDevices.Read.All`
   - `DeviceManagementApps.Read.All`
3. Add your credentials to `appsettings.json`:

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

---

## Roadmap

- [ ] Real Microsoft Graph API integration
- [ ] Teams and email notifications
- [ ] Multi-tenant support
- [ ] Per-device drill-down view
- [ ] Enrollment history and analytics
- [ ] Configurable required app lists per device group
