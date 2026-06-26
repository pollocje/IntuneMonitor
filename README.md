# IntuneMonitor

**Know the second a device is ready. Fix it remotely when it's not.**

When you reimage a laptop and enroll it in Intune, there's no native way to know when all the required apps have finished installing. You're left refreshing the portal manually, guessing, and fielding calls from users asking "is my computer ready yet?"

IntuneMonitor fixes that. It watches your Intune enrollments in real time, notifies you the moment a device is fully set up, and lets you remotely trigger fixes when apps are stuck — no BeyondTrust or TeamViewer required.

---

## Features

- **Live enrollment dashboard** — see every device currently enrolling with per-app install progress bars
- **Real-time updates** — dashboard refreshes automatically via SignalR, no page reloads
- **Teams notifications** — Adaptive Card fired to your Teams channel the moment a device is ready
- **Stuck/failed app detection** — automatic warning banner when apps fail to install
- **Force Sync** — remotely trigger an immediate Intune sync on any device, no RMM needed
- **Restart IME Service** — remotely restart the IntuneManagementExtension Windows service via Proactive Remediation, without touching the device
- **Device detail view** — drill into any device for full app status, enrollment timeline, and actions
- **Settings page** — configure Teams webhook and notification email from inside the app
- **Dashboard nav** — sticky nav with user email, settings link, sign out
- **User accounts** — sign up, log in, 14-day free trial, cookie-based auth
- **Stripe subscriptions** — $49/month, full checkout and billing lifecycle handled automatically

---

## Tech Stack

- [ASP.NET Core](https://dotnet.microsoft.com/) — backend
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — frontend (no JavaScript framework)
- [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) — real-time dashboard updates
- [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview) — Intune device/app data, device actions, Proactive Remediations
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) + PostgreSQL — persistence and multi-tenant data
- [Stripe.net](https://stripe.com/docs/api) — subscription billing
- [BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) — password hashing

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (or run without a DB — auth/billing features will be unavailable)
- A Microsoft tenant with Intune (or use built-in mock data for development)

### Run locally

```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet new blazorserver --force   # generates required Blazor boilerplate
dotnet restore
dotnet run
```

Open `http://localhost:5000` — landing page at `/`, dashboard at `/dashboard`.

Runs against **animated mock data** by default. Includes a stuck device (`LAPTOP-STUCK99`) with failed apps to demonstrate the warning banner and action buttons.

### Set up the database

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Update the connection string in `appsettings.json` first:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=intunemonitor;Username=postgres;Password=yourpassword"
  }
}
```

### Connect to a real Intune tenant

1. Register an app in [Azure Portal](https://portal.azure.com) → App registrations
2. Add API permissions (application):
   - `DeviceManagementManagedDevices.Read.All`
   - `DeviceManagementApps.Read.All`
   - `DeviceManagementManagedDevices.ReadWrite.All` ← required for Force Sync
   - `DeviceManagementManagedDevices.PrivilegedOperations.All` ← required for Restart IME
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

> **Note:** Restart IME Service requires Intune Plan 2 / M365 E3+ on the customer's tenant. The remediation script is created automatically during the tenant onboarding flow (not yet built).

### Enable Teams notifications

```json
{
  "Notifications": {
    "TeamsWebhookUrl": "https://your-webhook-url"
  }
}
```

### Enable Stripe billing

1. Create a product + recurring price in your [Stripe dashboard](https://dashboard.stripe.com)
2. Add a webhook endpoint pointing to `https://yourdomain.com/billing/webhook`
3. Fill in `appsettings.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_live_...",
    "WebhookSecret": "whsec_...",
    "PriceId": "price_..."
  },
  "AppUrl": "https://yourdomain.com"
}
```

---

## Project Structure

```
IntuneMonitor/
├── Data/
│   ├── AppDbContext.cs
│   └── Entities/              # Tenant, AppUser, EnrollmentRecord
├── Hubs/
│   └── EnrollmentHub.cs       # SignalR hub
├── Models/                    # DeviceEnrollment, AppInstallStatus (IsFailed computed)
├── Pages/
│   ├── Index.razor            # Landing / marketing page (/)
│   ├── Dashboard.razor        # Live enrollment dashboard (/dashboard)
│   ├── DeviceDetail.razor     # Per-device detail + Force Sync / Restart IME (/device/{id})
│   ├── Settings.razor         # Notification config, subscription status (/settings)
│   ├── Account/
│   │   ├── Login.cshtml       # Sign in (/account/login)
│   │   ├── Signup.cshtml      # Create account (/account/signup)
│   │   └── Logout.cshtml.cs   # Sign out — supports GET and POST
│   └── Billing/
│       ├── Checkout.cshtml.cs # Redirects to Stripe (/billing/checkout)
│       └── Success.cshtml     # Post-payment confirmation (/billing/success)
├── Services/
│   ├── IGraphService.cs       # GetRecentEnrollmentsAsync, SyncDeviceAsync, RestartImeServiceAsync
│   ├── MockGraphService.cs    # Animated mock — includes stuck/failed device for demo
│   ├── GraphService.cs        # Real Graph API — syncDevice + initiateOnDemandProactiveRemediation
│   ├── INotificationService.cs
│   ├── TeamsNotificationService.cs
│   ├── AuthService.cs         # Sign up, login, BCrypt password hashing
│   └── StripeService.cs       # Checkout sessions, webhook handling
├── Shared/
│   ├── DashboardLayout.razor  # Sticky nav (logo, links, user email, sign out)
│   ├── SummaryCards.razor     # Stat cards (Total/Waiting/Installing/Ready)
│   ├── SubscriptionGate.razor # Blocks dashboard if trial/subscription expired
│   ├── RedirectToLogin.razor  # Auth redirect component
│   └── EmptyLayout.razor      # Layout for public pages
├── Workers/
│   └── EnrollmentMonitorWorker.cs  # Background polling + notifications
└── App.razor                  # CascadingAuthenticationState wrapper
```

---

## Roadmap

- [x] Live enrollment dashboard with real-time SignalR updates
- [x] Animated mock data with stuck/failed device for demo
- [x] Teams webhook notifications (Adaptive Cards)
- [x] Device detail page
- [x] Summary stat cards
- [x] Database schema (EF Core + PostgreSQL)
- [x] Real Graph API implementation
- [x] Landing / marketing page
- [x] User auth — sign up, login, logout, BCrypt, cookie sessions
- [x] Stripe subscriptions — checkout, webhooks, subscription gate
- [x] Dashboard nav bar and settings page
- [x] Force Sync — remotely trigger Intune sync via Graph API
- [x] Restart IME Service — via Intune Proactive Remediation, no RMM needed
- [ ] Microsoft OAuth tenant connect flow
- [ ] Auto-create IME remediation script during tenant onboarding
- [ ] Multi-tenant worker (poll all connected tenants)
- [ ] Email notifications
- [ ] Azure deployment
- [ ] Microsoft app verification (removes "unverified app" warning)
