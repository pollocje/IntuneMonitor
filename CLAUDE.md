# IntuneMonitor — Project Context

## What this is
A SaaS product that monitors Microsoft Intune device enrollments in real time. When a laptop is reimaged and enrolled in Intune, IT admins have no way to know when all the required apps have finished installing — they just wait and guess. This app watches the enrollment, shows a live dashboard of app install progress, and sends a Teams notification the moment a device is fully ready.

Target customers: IT admins at SMBs, MSPs who reprovision devices regularly.

## Who is building this
Solo project by a final-year CS student doing an IT co-op at a provincial government (Canada). Primary language is C#. Building this as a side project to eventually market and sell as a SaaS.

## Tech stack
- **ASP.NET Core** + **Blazor Server** — backend and frontend (no JavaScript framework)
- **SignalR** — real-time dashboard updates pushed to browser
- **Microsoft Graph API** — pulls Intune device and app install status data
- **Entity Framework Core** + **PostgreSQL** — persistence, multi-tenant data model
- **BCrypt.Net-Next** — password hashing
- **Stripe.net** — subscription billing
- **Teams webhook (Adaptive Cards)** — device ready notifications

## Current state
The project is a full working skeleton. Runs against animated mock data with no external dependencies. All major SaaS infrastructure is wired up as skeletons ready to activate with real credentials.

### What's built
- `Models/` — `DeviceEnrollment`, `AppInstallStatus`
- `Services/IGraphService` + `MockGraphService` — animated mock (apps install one at a time, new devices spawn when all ready). Singleton so state persists between polls.
- `Services/GraphService` — real Graph API implementation using `ClientSecretCredential`. Swap in via `Program.cs` when credentials are ready.
- `Services/INotificationService` + `TeamsNotificationService` — sends Adaptive Card to Teams webhook. Gracefully skips if no URL configured.
- `Services/AuthService` — BCrypt password hashing, creates Tenant + AppUser on registration, 14-day trial
- `Services/StripeService` — creates Stripe Checkout sessions, handles all webhook events (subscription created/updated/deleted, payment succeeded/failed), updates `Tenant.SubscriptionStatus`
- `Workers/EnrollmentMonitorWorker` — polls every 10s, pushes to SignalR hub, fires notifications. Tracks notified devices in-memory `HashSet` (resets on restart — acceptable for now)
- `Hubs/EnrollmentHub` — SignalR hub
- `Data/AppDbContext` + `Entities/` — EF Core with Tenant, AppUser, EnrollmentRecord. Tenant has StripeCustomerId, StripeSubscriptionId, SubscriptionStatus, TrialEndsAt.
- `Pages/Index.razor` — full marketing landing page at `/` (hero, pain points, features, how it works, pricing, footer). Uses `EmptyLayout` to skip Blazor chrome.
- `Pages/Dashboard.razor` — at `/dashboard`, `[Authorize]`, wrapped in `SubscriptionGate`
- `Pages/DeviceDetail.razor` — at `/device/{id}`, `[Authorize]`
- `Pages/Account/Login.cshtml` + `Signup.cshtml` + `Logout.cshtml.cs` — Razor Pages (not Blazor) for proper cookie auth HTTP redirects
- `Pages/Billing/Checkout.cshtml.cs` — GET redirect to Stripe Checkout
- `Pages/Billing/Success.cshtml` — post-payment confirmation page
- `Shared/SummaryCards.razor` — 4 stat cards
- `Shared/SubscriptionGate.razor` — checks tenant subscription status from DB, shows upgrade prompt if expired
- `Shared/RedirectToLogin.razor` — used by `App.razor` to redirect unauthenticated Blazor routes
- `App.razor` — `CascadingAuthenticationState` + `AuthorizeRouteView` wrapper
- `_Imports.razor` — global usings including auth namespaces

### What's NOT built yet
- Microsoft OAuth consent flow — customers connecting their own tenant (the "Connect your Microsoft tenant" button)
- Multi-tenant worker — currently polls one hardcoded mock, needs to loop over all DB tenants
- Email notifications (only Teams webhook exists)
- Azure deployment
- Microsoft app verification (removes "unverified app" warning customers see during OAuth consent)

## How to run locally

```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet new blazorserver --force   # generates _Host.cshtml, MainLayout, wwwroot etc.
dotnet restore
dotnet run
```

Landing page at `http://localhost:5000`, dashboard at `/dashboard`.

**First run will fail if DB is not set up** — either configure Postgres or comment out the `AddDbContext` line in `Program.cs` to run mock-only.

## Setting up the database
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```
Update connection string in `appsettings.json` first.

## Activating real features

| Feature | What to do |
|---|---|
| Real Intune data | Fill `AzureAd` config, swap `MockGraphService` → `GraphService` in `Program.cs` |
| Teams notifications | Paste webhook URL into `Notifications:TeamsWebhookUrl` |
| Stripe billing | Fill `Stripe` config keys + `AppUrl`, add webhook in Stripe dashboard pointing to `/billing/webhook` |

## Architecture notes
- Login/Signup/Logout are **Razor Pages** (`.cshtml`), not Blazor — required for cookie `SignInAsync` to issue HTTP redirects properly. Blazor components can't do this over WebSocket.
- `SubscriptionGate` uses `[CascadingParameter] Task<AuthenticationState>` to get TenantId from claims, then queries DB directly. It's scoped so each circuit gets its own instance.
- `MockGraphService` is registered as **Singleton** intentionally — so the animation state persists between the worker's polls.
- Stripe webhook endpoint uses `AllowAnonymous()` and reads raw body before any middleware — required for Stripe signature verification.
- Scoped CSS (`.razor.css`) is used throughout — Blazor automatically scopes styles to the component.

## Next priorities
1. Microsoft OAuth tenant connect flow — the core onboarding step
2. Multi-tenant worker — loop over all tenants in DB, poll each one's Graph API
3. Get a real test tenant and run GraphService against it
4. Email notifications
5. Deploy to Azure App Service
