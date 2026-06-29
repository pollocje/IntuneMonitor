# IntuneMonitor

**Know the second a device is ready. Fix it remotely when it's not.**

When you reimage a laptop and enroll it in Intune, there's no native way to know when all the required apps have finished installing. You're left refreshing the portal manually, guessing, and fielding calls from users asking "is my computer ready yet?"

IntuneMonitor fixes that. It watches your Intune enrollments in real time, notifies you the moment a device is fully set up, and lets you remotely trigger fixes when apps are stuck — no BeyondTrust or TeamViewer required.

---

## Features

**Monitoring**
- Live dashboard — per-device app install progress bars, updates in real time via SignalR
- Enrollment history — completed devices, time-to-ready stats (avg, fastest, slowest)
- Stuck/failed app detection with automatic warning banner on the device detail page

**Remote Actions** *(no RMM or BeyondTrust needed)*
- Force Sync — trigger an immediate Intune sync on any device
- Restart IME Service — restart IntuneManagementExtension via Proactive Remediation (requires Intune Plan 2 / M365 E3+)

**Notifications**
- Teams Adaptive Card when a device is ready
- Slack Block Kit message when a device is ready
- Email via SendGrid when a device is ready
- All three are per-tenant, configured in the app Settings page

**Tenant Setup**
- One-click Microsoft tenant connect via admin consent — every permission explained before you click
- IME remediation script auto-created in your tenant during onboarding

**Account & Billing**
- Sign up, login, 14-day free trial — no credit card required
- Forgot password flow — reset link sent via email, 1-hour expiry
- $10/month Stripe subscription with full checkout and billing lifecycle
- Trial expiry reminder — email sent automatically 3 days before trial ends
- Guided onboarding flow after signup — connect tenant, set up notifications, progress bar

---

## Tech Stack

- [ASP.NET Core](https://dotnet.microsoft.com/) + [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — backend and frontend
- [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) — real-time dashboard updates, scoped to tenant groups
- [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview) — Intune device/app data, device actions, Proactive Remediations
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) + PostgreSQL — multi-tenant persistence
- [Stripe.net](https://stripe.com/docs/api) — subscription billing
- [SendGrid](https://sendgrid.com/) — email notifications
- [BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) — password hashing

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (or a free cloud DB like [Neon](https://neon.tech))
- A Microsoft tenant with Intune (or use built-in mock data for development)

### Run locally (mock mode — no accounts needed)

```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet restore
dotnet run
```

Open `http://localhost:5000`. Mock mode runs automatically when no real tenant is connected — includes an animated enrollment demo and a stuck device (`LAPTOP-STUCK99`) with failed apps.

> **DB note:** First run will fail if Postgres isn't configured. Comment out `AddDbContext` in `Program.cs` to run fully mock with no database.

### Set up the database

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddNewFields
dotnet ef database update
```

> `AddNewFields` picks up all schema additions: Slack webhook, trial reminder tracking, and password reset token fields. If `InitialCreate` was already run, just run `AddNewFields`.

`appsettings.json` connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=intunemonitor;Username=postgres;Password=yourpassword"
  }
}
```

### Connect to a real Intune tenant

1. Register a **multi-tenant** app in [Azure Portal](https://portal.azure.com) → App registrations
   - Supported account types: **Accounts in any organizational directory**
   - Redirect URI (Web): `https://yourdomain.com/connect-callback`
2. Add API permissions (Application type):

| Permission | Used for |
|---|---|
| `DeviceManagementManagedDevices.Read.All` | Read device enrollment data |
| `DeviceManagementApps.Read.All` | Read app install status |
| `DeviceManagementManagedDevices.ReadWrite.All` | Force Sync |
| `DeviceManagementManagedDevices.PrivilegedOperations.All` | Restart IME Service |
| `DeviceManagementConfiguration.ReadWrite.All` | Create IME remediation script during onboarding |

3. Fill in `appsettings.json`:
```json
{
  "AzureAd": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "AppUrl": "https://yourdomain.com"
}
```

4. Customers go to **Settings → Connect Microsoft Tenant** → grant consent → tenant is connected, IME script auto-created

> Restart IME Service requires Intune Plan 2 / M365 E3+. If absent, the button shows "Not Configured" in the UI.

### Enable notifications

All notification channels are configured **per-tenant in the Settings UI** — not in appsettings.json.

**Teams:** Settings → Teams Webhook URL
- In Teams: channel → ··· → Connectors → Incoming Webhook → Create → copy URL

**Slack:** Settings → Slack Webhook URL
- In Slack: Apps → Incoming Webhooks → Add → choose channel → copy `https://hooks.slack.com/services/...` URL

**Email:** Settings → Notification Email — enter recipient address, then configure SendGrid in appsettings.json:
```json
{
  "SendGrid": {
    "ApiKey": "SG...",
    "FromEmail": "notifications@yourdomain.com"
  }
}
```

### Enable Stripe billing

1. Create a product + recurring price ($10/month) in [Stripe dashboard](https://dashboard.stripe.com)
2. Add webhook endpoint: `https://yourdomain.com/billing/webhook`
   - Events: `customer.subscription.*`, `invoice.payment_failed`, `invoice.payment_succeeded`
3. Fill in `appsettings.json`:
```json
{
  "Stripe": {
    "SecretKey": "sk_live_...",
    "WebhookSecret": "whsec_...",
    "PriceId": "price_..."
  }
}
```

### Admin dashboard

Navigate to `/admin` while logged in with an email listed in `appsettings.json`:
```json
{
  "Admin": {
    "Emails": [ "you@example.com" ]
  }
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
│   └── EnrollmentHub.cs       # SignalR hub — tenant-scoped groups
├── Models/                    # DeviceEnrollment, AppInstallStatus
├── Pages/
│   ├── Index.razor            # Landing / marketing page (/)
│   ├── Privacy.razor          # Privacy policy (/privacy)
│   ├── Terms.razor            # Terms of service (/terms)
│   ├── ConnectTenant.razor    # Microsoft OAuth connect UI (/connect-tenant)
│   ├── ConnectCallback.cshtml # OAuth callback handler (/connect-callback)
│   ├── Dashboard.razor        # Live enrollment dashboard (/dashboard)
│   ├── History.razor          # Completed enrollment history + stats (/history)
│   ├── DeviceDetail.razor     # Per-device detail + Force Sync / Restart IME (/device/{id})
│   ├── Onboarding.razor       # Post-signup setup checklist (/onboarding)
│   ├── Settings.razor         # Notifications, subscription, tenant connect (/settings)
│   ├── Admin/
│   │   └── Index.razor        # SaaS admin — all tenants, MRR, status (/admin)
│   ├── Account/
│   │   ├── Login.cshtml           # Sign in (/account/login)
│   │   ├── Signup.cshtml          # Create account (/account/signup)
│   │   ├── Logout.cshtml.cs       # Sign out
│   │   ├── ForgotPassword.cshtml  # Request reset link (/account/forgot-password)
│   │   └── ResetPassword.cshtml   # Set new password (/account/reset-password?token=...)
│   └── Billing/
│       ├── Checkout.cshtml.cs # Redirects to Stripe (/billing/checkout)
│       └── Success.cshtml     # Post-payment confirmation (/billing/success)
├── Services/
│   ├── IGraphService.cs
│   ├── MockGraphService.cs    # Animated mock with stuck device demo
│   ├── GraphService.cs        # Real Graph API implementation
│   ├── GraphServiceFactory.cs # Creates per-tenant GraphService instances
│   ├── INotificationService.cs
│   ├── NotificationService.cs # Composite — calls Teams + Slack + Email
│   ├── TeamsNotificationService.cs
│   ├── SlackNotificationService.cs
│   ├── EmailNotificationService.cs
│   ├── AuthService.cs
│   ├── StripeService.cs
│   └── TenantOnboardingService.cs
├── Shared/
│   ├── DashboardLayout.razor  # Sticky nav (Dashboard, History, Settings)
│   ├── SummaryCards.razor
│   ├── SubscriptionGate.razor
│   ├── RedirectToLogin.razor
│   └── EmptyLayout.razor
├── Workers/
│   ├── EnrollmentMonitorWorker.cs  # Multi-tenant polling, mock fallback
│   └── TrialReminderWorker.cs      # Hourly — sends trial expiry warning 3 days before end
└── App.razor
```

---

## Roadmap

- [x] Live enrollment dashboard with real-time SignalR updates
- [x] Multi-tenant worker — polls all connected tenants, mock fallback when none connected
- [x] Animated mock data with stuck/failed device for demo
- [x] Device detail page with Force Sync and Restart IME actions
- [x] Summary stat cards
- [x] Enrollment history page with time-to-ready stats
- [x] Database schema (EF Core + PostgreSQL)
- [x] Real Graph API — per-tenant via GraphServiceFactory
- [x] Landing / marketing page
- [x] User auth — sign up, login, logout, BCrypt, cookie sessions
- [x] Stripe subscriptions — checkout, webhooks, subscription gate
- [x] Settings page — notifications, subscription, tenant connect
- [x] Teams webhook notifications (Adaptive Cards, per-tenant)
- [x] Slack webhook notifications (Block Kit, per-tenant)
- [x] Email notifications via SendGrid (per-tenant)
- [x] Microsoft OAuth tenant connect flow — admin consent, permission disclosure
- [x] Auto-create IME remediation script during tenant onboarding
- [x] Admin dashboard — tenant list, MRR, trial countdowns
- [x] Guided onboarding flow after signup
- [x] Privacy policy + Terms of Service
- [x] Password reset flow — forgot password, email link, 1-hour expiry
- [x] Trial expiry warning email — automated 3-day reminder
- [ ] Azure deployment
- [ ] Microsoft Publisher Verification (removes "unverified app" warning)
- [ ] DeviceDetail Force Sync / Restart IME for real tenants (currently uses MockGraphService)
