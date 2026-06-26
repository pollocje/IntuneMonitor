# IntuneMonitor — Project Context

## What this is
A SaaS product that monitors Microsoft Intune device enrollments in real time. When a laptop is reimaged and enrolled in Intune, IT admins have no way to know when all the required apps have finished installing — they just wait and guess. This app watches the enrollment, shows a live dashboard, notifies the admin when a device is ready, and lets them remotely fix stuck devices without needing BeyondTrust or TeamViewer.

Target customers: IT admins at SMBs, MSPs who reprovision devices regularly.

## Who is building this
Solo project by a final-year CS student doing an IT co-op at a provincial government (Canada). Primary language is C#. Building this as a side project to eventually market and sell as a SaaS.

## Tech stack
- **ASP.NET Core** + **Blazor Server** — backend and frontend (no JavaScript framework)
- **SignalR** — real-time dashboard updates pushed to browser
- **Microsoft Graph API** — Intune device/app data, `syncDevice`, `initiateOnDemandProactiveRemediation`
- **Entity Framework Core** + **PostgreSQL** — persistence, multi-tenant data model
- **BCrypt.Net-Next** — password hashing
- **Stripe.net** — subscription billing
- **Teams webhook (Adaptive Cards)** — device ready notifications

## Current state
Full working skeleton running against animated mock data. All major SaaS infrastructure is in place. No external dependencies needed to run and demo the full UI.

### What's built

**Models**
- `DeviceEnrollment` — device state, computed `IsFullyEnrolled`, `StatusLabel`, `TimeToReady`
- `AppInstallStatus` — per-app install state, computed `IsFailed` (InstallState == "failed")

**Services**
- `IGraphService` — `GetRecentEnrollmentsAsync`, `SyncDeviceAsync`, `RestartImeServiceAsync`
- `MockGraphService` — singleton, animates apps installing one at a time. Includes `device-004` (LAPTOP-STUCK99) with two failed apps to demo the stuck/warning UI. `SyncDeviceAsync` resets failed apps to notInstalled.
- `GraphService` — real Graph API: `syncDevice` POST, `initiateOnDemandProactiveRemediation` POST
- `AuthService` — BCrypt hashing, creates Tenant + AppUser on signup, 14-day trial
- `StripeService` — Stripe Checkout sessions, handles all webhook events, updates `Tenant.SubscriptionStatus`
- `TeamsNotificationService` — Adaptive Card to Teams webhook, skips gracefully if not configured

**Workers**
- `EnrollmentMonitorWorker` — polls every 10s, pushes to SignalR hub, fires notifications. Tracks notified devices in `HashSet` (resets on restart).

**Data**
- `Tenant` — MicrosoftTenantId, StripeCustomerId, StripeSubscriptionId, SubscriptionStatus, TrialEndsAt, TeamsWebhookUrl, NotificationEmail, **RemediationScriptId** (set during onboarding when IME remediation script is created)
- `AppUser` — email, password hash, role, tenantId FK
- `EnrollmentRecord` — history of completed enrollments with TimeToReady

**Pages**
- `Index.razor` — full marketing landing page at `/`, uses `EmptyLayout`
- `Dashboard.razor` — `/dashboard`, `[Authorize]`, `@layout DashboardLayout`, wrapped in `SubscriptionGate`
- `DeviceDetail.razor` — `/device/{id}`, `[Authorize]`, `@layout DashboardLayout`
  - Shows "Stuck" badge and yellow warning banner when any app has `InstallState == "failed"`
  - **Force Sync** button — always shown for non-ready devices, calls `SyncDeviceAsync`
  - **Restart IME Service** button — shown only if `Tenant.RemediationScriptId` is set, calls `RestartImeServiceAsync`. Shows "Not Configured" state with settings link if not set up.
  - Buttons disable during action, show confirmation text after success, show error on failure
- `Settings.razor` — `/settings`, `[Authorize]`, `@layout DashboardLayout`
  - Account info, subscription status with trial days remaining, Teams webhook + notification email fields with individual Save buttons, Microsoft tenant connect status
- `Account/Login.cshtml`, `Signup.cshtml`, `Logout.cshtml.cs` — Razor Pages (not Blazor) for cookie auth. Logout supports both GET and POST.
- `Billing/Checkout.cshtml.cs` — GET redirects to Stripe Checkout. `Billing/Success.cshtml` — post-payment page.

**Shared**
- `DashboardLayout.razor` — sticky nav (logo → `/`, Dashboard, Settings links, user email, Sign Out). Used by Dashboard, DeviceDetail, Settings.
- `SummaryCards.razor` — 4 stat cards
- `SubscriptionGate.razor` — checks tenant subscription from DB, shows upgrade card if expired
- `RedirectToLogin.razor` — used by `App.razor` for unauthenticated Blazor routes
- `EmptyLayout.razor` — used by Index.razor (landing page)
- `App.razor` — `CascadingAuthenticationState` + `AuthorizeRouteView`
- `_Imports.razor` — global usings including auth namespaces

### What's NOT built yet
- Microsoft OAuth tenant connect flow — customers click "Connect your tenant", grant permissions, we store their tenant credentials
- Auto-create IME remediation script during onboarding — during connect flow, create the "Restart IME Service" Proactive Remediation in their Intune, store the script ID in `Tenant.RemediationScriptId`
- Multi-tenant worker — currently polls mock data. Needs to loop over all DB tenants and poll each one's Graph API
- Email notifications (only Teams webhook exists)
- Azure deployment
- Microsoft app verification (removes "unverified app" consent warning)

## How to run locally

```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet new blazorserver --force
dotnet restore
dotnet run
```

Landing page at `http://localhost:5000`, dashboard at `/dashboard`.

**First run fails if DB not configured** — comment out `AddDbContext` in `Program.cs` to run mock-only without Postgres.

## Activating real features

| Feature | What to do |
|---|---|
| Real Intune data | Fill `AzureAd` config, swap `MockGraphService` → `GraphService` in `Program.cs` |
| Force Sync | Requires `DeviceManagementManagedDevices.ReadWrite.All` permission |
| Restart IME | Requires `DeviceManagementManagedDevices.PrivilegedOperations.All` + Intune Plan 2/M365 E3+ |
| Teams notifications | Paste webhook URL into `Notifications:TeamsWebhookUrl` |
| Stripe billing | Fill `Stripe` config + `AppUrl`, add `/billing/webhook` in Stripe dashboard |

## Architecture notes
- Login/Signup/Logout are **Razor Pages** — required for cookie `SignInAsync` HTTP redirects. Blazor can't do this over WebSocket.
- `MockGraphService` is **Singleton** — so animation state persists between worker polls.
- `SubscriptionGate` and `DeviceDetail` use `[CascadingParameter] Task<AuthenticationState>` to get TenantId from claims, then query DB directly.
- Stripe webhook uses `AllowAnonymous()` and reads raw body before middleware — required for signature verification.
- `RestartImeServiceAsync` uses Intune Proactive Remediations (`initiateOnDemandProactiveRemediation`) — NOT a full device reboot. Restarts only the IntuneManagementExtension Windows service.
- `Tenant.RemediationScriptId` is null until the OAuth tenant connect flow creates the script during onboarding.
- Scoped CSS (`.razor.css`) used throughout — Blazor automatically scopes styles.

## Next priorities
1. Microsoft OAuth tenant connect flow (biggest remaining feature)
2. Auto-create IME remediation script in customer's tenant during onboarding
3. Multi-tenant worker
4. Email notifications
5. Azure deployment
