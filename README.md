# IntuneMonitor

**Know the second a device is ready. Stop babysitting Intune.**

When you reimage a laptop and enroll it in Intune, there's no native way to know when all the required apps have finished installing. You're left refreshing the portal manually, guessing, and fielding calls from users asking "is my computer ready yet?"

IntuneMonitor fixes that. It watches your Intune enrollments in real time and notifies you the moment a device is fully set up — every required app installed, no manual checking required.

---

## Features

- **Live enrollment dashboard** — see every device currently enrolling with per-app install progress bars
- **Real-time updates** — dashboard refreshes automatically via SignalR, no page reloads
- **Teams notifications** — Adaptive Card fired to your Teams channel the moment a device is ready
- **Failure visibility** — see which apps are stuck or failed immediately
- **Device detail view** — drill into any device to see full app status and enrollment timeline
- **Enrollment history** — track how long enrollments take across your fleet over time
- **User accounts** — sign up, log in, 14-day free trial, cookie-based auth
- **Stripe subscriptions** — $49/month, full checkout and billing lifecycle handled automatically

---

## Tech Stack

- [ASP.NET Core](https://dotnet.microsoft.com/) — backend
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — frontend (no JavaScript framework)
- [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) — real-time dashboard updates
- [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview) — Intune device and app data
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) + PostgreSQL — persistence and multi-tenant data
- [Stripe.net](https://stripe.com/docs/api) — subscription billing

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

Runs against **animated mock data** by default. No Intune tenant or database required to explore the UI.

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
├── Models/                    # DeviceEnrollment, AppInstallStatus
├── Pages/
│   ├── Index.razor            # Landing / marketing page (/)
│   ├── Dashboard.razor        # Live enrollment dashboard (/dashboard)
│   ├── DeviceDetail.razor     # Per-device detail view (/device/{id})
│   ├── Account/
│   │   ├── Login.cshtml       # Sign in (/account/login)
│   │   ├── Signup.cshtml      # Create account (/account/signup)
│   │   └── Logout.cshtml.cs   # Sign out (POST /account/logout)
│   └── Billing/
│       ├── Checkout.cshtml.cs # Redirects to Stripe (/billing/checkout)
│       └── Success.cshtml     # Post-payment confirmation (/billing/success)
├── Services/
│   ├── IGraphService.cs
│   ├── MockGraphService.cs    # Animated mock — no tenant needed
│   ├── GraphService.cs        # Real Graph API implementation
│   ├── INotificationService.cs
│   ├── TeamsNotificationService.cs
│   ├── AuthService.cs         # Sign up, login, BCrypt password hashing
│   └── StripeService.cs       # Checkout sessions, webhook handling
├── Shared/
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
- [x] Animated mock data for development
- [x] Teams webhook notifications (Adaptive Cards)
- [x] Device detail page
- [x] Summary stat cards
- [x] Database schema (EF Core + PostgreSQL)
- [x] Real Graph API implementation
- [x] Landing / marketing page
- [x] User auth — sign up, login, logout, BCrypt, cookie sessions
- [x] Stripe subscriptions — checkout, webhooks, subscription gate
- [ ] Microsoft OAuth tenant connect flow
- [ ] Multi-tenant worker (poll all connected tenants)
- [ ] Email notifications
- [ ] Azure deployment
- [ ] Microsoft app verification (removes "unverified app" warning)
