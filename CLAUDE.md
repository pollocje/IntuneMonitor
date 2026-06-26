# IntuneMonitor — Project Context

## What this is
A SaaS product that monitors Microsoft Intune device enrollments in real time. When a laptop is reimaged and enrolled in Intune, IT admins have no way to know when all the required apps have finished installing — they just wait and guess. This app watches the enrollment, shows a live dashboard, notifies the admin when a device is ready, and lets them remotely fix stuck devices without needing BeyondTrust or TeamViewer.

Target customers: IT admins at SMBs, MSPs who reprovision devices regularly. $10/month.

## Who is building this
Solo project by a final-year CS student doing an IT co-op at a provincial government (Canada). Primary language is C#. Building this as a side project to eventually market and sell as a SaaS. Personal email: jeffrey.pollock123@gmail.com (used for admin access and git commits).

## Tech stack
- **ASP.NET Core** + **Blazor Server** — backend and frontend (no JavaScript framework)
- **SignalR** — real-time dashboard updates pushed to browser, scoped to tenant groups
- **Microsoft Graph API** — Intune device/app data, `syncDevice`, `initiateOnDemandProactiveRemediation`
- **Entity Framework Core** + **PostgreSQL** — persistence, multi-tenant data model
- **BCrypt.Net-Next** — password hashing
- **Stripe.net** — subscription billing
- **SendGrid** — email notifications
- **Teams + Slack incoming webhooks** — device ready notifications

---

## What's built

### Models
- `DeviceEnrollment` — device state, computed `IsFullyEnrolled`, `StatusLabel`, `TimeToReady`
- `AppInstallStatus` — per-app install state, computed `IsFailed` (InstallState == "failed")

### Services
- `IGraphService` — `GetRecentEnrollmentsAsync`, `SyncDeviceAsync`, `RestartImeServiceAsync`
- `MockGraphService` (Singleton) — animates apps installing one at a time. Includes `device-004` (LAPTOP-STUCK99) with two failed apps to demo the stuck/warning UI.
- `GraphService` — real Graph API: takes `GraphServiceClient` in constructor (injected by factory). `syncDevice` POST, `initiateOnDemandProactiveRemediation` POST.
- `GraphServiceFactory` (Singleton) — creates per-tenant `GraphService` instances using our `ClientId`/`ClientSecret` scoped to the customer's `MicrosoftTenantId`. `IsConfigured` returns false if no credentials in config (triggers mock fallback in worker).
- `AuthService` — BCrypt hashing, creates Tenant + AppUser on signup, 14-day trial
- `StripeService` — Stripe Checkout sessions, handles all webhook events, updates `Tenant.SubscriptionStatus`
- `INotificationService` — `Task SendDeviceReadyAsync(DeviceEnrollment device, Tenant? tenant = null)`. Tenant is null in mock mode.
- `NotificationService` (composite, implements `INotificationService`) — calls Teams, Slack, and Email in sequence. Registered as `INotificationService`.
- `TeamsNotificationService` — Adaptive Card POST to `tenant.TeamsWebhookUrl`. Skips if null.
- `SlackNotificationService` — Block Kit message POST to `tenant.SlackWebhookUrl`. Skips if null. Includes header, 4-field grid (user, apps, enrolled, time to ready), primary "View Device →" button.
- `EmailNotificationService` — SendGrid. Sends to `tenant.NotificationEmail`. HTML email with device info table and dashboard link. Skips if no API key or no recipient.
- `TenantOnboardingService` — called after admin consent. Creates `DeviceHealthScript` (Proactive Remediation: detection checks if IME running, remediation restarts it) in customer's tenant. Sets `Tenant.RemediationScriptId`. Requires Intune Plan 2 / M365 E3+; failure is non-fatal.

### Workers
- `EnrollmentMonitorWorker` — polls every 10s. Queries DB for tenants with `MicrosoftTenantId` set. If none found OR `GraphServiceFactory.IsConfigured == false`: uses `MockGraphService`, pushes to `Clients.All` (mock mode). Otherwise: creates a `GraphService` per tenant via `GraphServiceFactory`, polls real data, pushes to `Clients.Group("tenant-{tenantId}")`, fires notifications with tenant context, saves `EnrollmentRecord` to DB. Per-tenant `HashSet<string>` tracks notified devices; DB checked to avoid duplicates across restarts.

### Data
- `Tenant` — Id, Name, MicrosoftTenantId, CreatedAt, SubscriptionStatus (Trial/Active/PastDue/Cancelled), TrialEndsAt, StripeCustomerId, StripeSubscriptionId, TeamsWebhookUrl, **SlackWebhookUrl**, NotificationEmail, RemediationScriptId
- `AppUser` — Id, TenantId FK, Email, PasswordHash, Role (Admin/Viewer), CreatedAt, LastLoginAt
- `EnrollmentRecord` — Id, TenantId FK, DeviceId, DeviceName, UserPrincipalName, EnrolledAt, ReadyAt, NotificationSentAt, TotalApps, InstalledApps, AppStatusesJson. Computed: IsComplete, TimeToReady.

### Pages
- `Index.razor` — `/`, `@layout EmptyLayout`. Full marketing landing page.
- `Privacy.razor` — `/privacy`, `@layout EmptyLayout`. Full privacy policy (Canadian data storage, 90-day retention, Graph access, Stripe, deletion rights).
- `Terms.razor` — `/terms`, `@layout EmptyLayout`. Terms of service (Ontario governing law, $10/month, 14-day trial, acceptable use, liability).
- `ConnectTenant.razor` — `/connect-tenant`, `[Authorize]`. Explains all 5 permissions being requested. Builds admin consent URL with our `ClientId` and user's DB tenant GUID as `state`. Shows config warning if `AzureAd:ClientId` not set. Shows success/error states from query params.
- `ConnectCallback.cshtml.cs` — `/connect-callback`, `[Authorize]` Razor Page. Validates `admin_consent=True`, verifies `state` matches logged-in user's TenantId claim, saves `MicrosoftTenantId` to DB, calls `TenantOnboardingService.SetupTenantAsync`. IME script failure is non-fatal.
- `Onboarding.razor` — `/onboarding`, `[Authorize]`, `@layout EmptyLayout`. Post-signup checklist. 2 steps: (1) Connect Microsoft tenant, (2) Set up notifications. Progress bar fills as steps complete. Each step reads live state from DB — shows green check + details when done, action button when not. Step 2 grays out while step 1 is incomplete. "All set!" banner when both done. "Skip for now" in nav and footer always let user go to dashboard. Signup.cshtml.cs redirects here instead of `/dashboard` after registration.
- `Dashboard.razor` — `/dashboard`, `[Authorize]`, `@layout DashboardLayout`, wrapped in `SubscriptionGate`. Passes `?tenantId=` to hub URL so SignalR puts the connection in the right group.
- `History.razor` — `/history`, `[Authorize]`, `@layout DashboardLayout`. 4 stat cards (count, avg/fastest/slowest time to ready). Table with color-coded duration pills (green < 25 min, yellow < 45 min, red otherwise). Falls back to 7 hardcoded demo rows if DB is empty or unavailable, with a yellow banner noting it's sample data.
- `DeviceDetail.razor` — `/device/{id}`, `[Authorize]`. Stuck badge + warning banner when apps failed. Force Sync button (always shown). Restart IME button (shown only if `Tenant.RemediationScriptId` set, else "Not Configured" with settings link). Buttons disable during action, show confirmation/error.
- `Settings.razor` — `/settings`, `[Authorize]`. Account info, subscription status, Teams webhook, Slack webhook, notification email (each with own Save button + 3s confirmation), Microsoft tenant connect status.
- `Admin/Index.razor` — `/admin`, `[Authorize]`. Access controlled by `Admin:Emails` list in `appsettings.json` (not DB role). Shows: 4 stat cards (total, active, trial, MRR at $10×active). Table: email, status pill, MS connected, notification channels (Teams/Slack/Email badges), trial days left (red at ≤3), join date.
- `Account/Login.cshtml`, `Signup.cshtml`, `Logout.cshtml.cs` — Razor Pages for cookie auth. Logout supports GET + POST.
- `Billing/Checkout.cshtml.cs`, `Success.cshtml` — Stripe Checkout redirect + success page.

### Shared
- `DashboardLayout.razor` — sticky nav: logo → `/`, Dashboard, History, Settings links, user email, Sign Out.
- `SummaryCards.razor` — 4 stat cards (Total/Waiting/Installing/Ready)
- `SubscriptionGate.razor` — blocks dashboard if trial expired AND not Active
- `EnrollmentHub.cs` — SignalR hub. `OnConnectedAsync` reads `?tenantId=` from query string, adds connection to group `tenant-{tenantId}`. `OnDisconnectedAsync` removes from group.
- `RedirectToLogin.razor`, `EmptyLayout.razor`, `App.razor` — standard Blazor auth plumbing

---

## What's NOT built yet
- **Azure deployment** — needs to be hosted somewhere with a real URL
- **Microsoft Publisher Verification** — without it the admin consent screen shows "unverified app" warning. Requires a verified domain and Microsoft Partner Network account.
- **Pending DB migration** — `SlackWebhookUrl` column added to `Tenant` entity but migration not yet run. Run `dotnet ef migrations add AddSlackWebhookUrl && dotnet ef database update` before first real run.

---

## Home computer checklist
Everything needed to go from code → actually running with real tenants:

### 1. Install prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (or use a free cloud DB — Neon.tech is easy)
- [dotnet-ef tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

### 2. Clone and restore
```bash
git clone https://github.com/pollocje/IntuneMonitor.git
cd IntuneMonitor
dotnet restore
```

### 3. Configure appsettings.json
Fill in all blank fields:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=intunemonitor;Username=postgres;Password=yourpassword"
  },
  "Stripe": {
    "SecretKey": "sk_live_...",
    "WebhookSecret": "whsec_...",
    "PriceId": "price_..."
  },
  "SendGrid": {
    "ApiKey": "SG...",
    "FromEmail": "notifications@yourdomain.com"
  },
  "AzureAd": {
    "ClientId": "your-app-client-id",
    "ClientSecret": "your-app-client-secret"
  },
  "AppUrl": "https://yourdomain.com",
  "Admin": {
    "Emails": [ "jeffrey.pollock123@gmail.com" ]
  }
}
```

### 4. Run DB migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddSlackWebhookUrl
dotnet ef database update
```
> If you already ran `InitialCreate` previously, just run `AddSlackWebhookUrl`.

### 5. Set up Azure App Registration
1. Go to [portal.azure.com](https://portal.azure.com) → Azure Active Directory → App registrations → New registration
2. Name: `IntuneMonitor`
3. Supported account types: **Accounts in any organizational directory (multi-tenant)**
4. Redirect URI: `https://yourdomain.com/connect-callback` (Web)
5. After creating, go to API Permissions → Add permission → Microsoft Graph → Application permissions:
   - `DeviceManagementManagedDevices.Read.All`
   - `DeviceManagementApps.Read.All`
   - `DeviceManagementManagedDevices.ReadWrite.All`
   - `DeviceManagementManagedDevices.PrivilegedOperations.All`
   - `DeviceManagementConfiguration.ReadWrite.All`
6. Certificates & secrets → New client secret → copy the value into `AzureAd:ClientSecret`
7. Copy the Application (client) ID into `AzureAd:ClientId`

### 6. Set up Stripe
1. Create a product in [Stripe dashboard](https://dashboard.stripe.com) → Products → Add product
2. Add a recurring price: $10/month
3. Copy the Price ID (`price_...`) into `Stripe:PriceId`
4. Developers → API Keys → copy Secret Key into `Stripe:SecretKey`
5. Webhooks → Add endpoint: `https://yourdomain.com/billing/webhook`
   - Events to listen for: `customer.subscription.updated`, `customer.subscription.created`, `customer.subscription.deleted`, `invoice.payment_failed`, `invoice.payment_succeeded`
6. Copy webhook signing secret into `Stripe:WebhookSecret`

### 7. Set up SendGrid
1. Create account at [sendgrid.com](https://sendgrid.com)
2. Settings → API Keys → Create API Key (Full Access)
3. Copy key into `SendGrid:ApiKey`
4. Verify a sender email address (Settings → Sender Authentication)
5. Put that verified email in `SendGrid:FromEmail`

### 8. Set up Slack notifications (per-tenant, done in the app UI)
- In Slack: Apps → Incoming Webhooks → Add → pick a channel → copy `https://hooks.slack.com/services/...` URL
- Paste into Settings → Slack Webhook URL in the app

### 9. Test the mock mode first
```bash
dotnet run
```
- Open `http://localhost:5000`
- Sign up with `jeffrey.pollock123@gmail.com`
- Go to `/dashboard` — mock data should animate
- Go to `/admin` — should see your tenant listed

### 10. Test with a real tenant
- Go to Settings → Connect Microsoft Tenant
- Grant admin consent on your own test tenant
- Worker will detect the connected tenant and switch from mock → real Graph polling automatically

---

## Architecture notes
- Login/Signup/Logout and ConnectCallback are **Razor Pages** — required for cookie `SignInAsync`/HTTP redirects. Blazor can't do this over WebSocket.
- `MockGraphService` is **Singleton** — animation state persists between worker polls.
- Worker falls back to mock automatically: if no tenants in DB have `MicrosoftTenantId`, OR if `AzureAd:ClientId/ClientSecret` are blank, mock mode runs.
- SignalR tenant groups: Dashboard passes `?tenantId=` when connecting to `/enrollmenthub`. Hub adds that connection to `group "tenant-{id}"`. Worker pushes real data to the group, mock data to `Clients.All`.
- Notification channels are all per-tenant, set via the Settings UI — Teams, Slack, and email webhook URLs are stored on the `Tenant` entity in DB, NOT in appsettings.
- `NotificationService` is a composite that calls Teams → Slack → Email in sequence. All three skip gracefully if not configured.
- `TenantOnboardingService` creates `GraphServiceClient` scoped to *customer's* tenant using *our* credentials — works because admin consent adds our service principal to their tenant.
- `Admin:Emails` in appsettings controls who can access `/admin`. Add emails to the array. No DB role involved.
- `SlackWebhookUrl` column was added to `Tenant` after `InitialCreate` — run `AddSlackWebhookUrl` migration separately.
- `GraphService` constructor takes `GraphServiceClient` directly (not IConfiguration). Created by `GraphServiceFactory`, never registered directly in DI. `IGraphService` in DI still resolves to `MockGraphService` (used by `DeviceDetail.razor` for Force Sync / Restart IME actions — these will need updating for multi-tenant DeviceDetail later).

## Next priorities
1. **Azure deployment** — app needs a real URL before you can test OAuth consent or Stripe webhooks end-to-end
2. **Microsoft Publisher Verification** — removes "unverified app" warning on consent screen
3. **Trial expiry warning email** — background job that fires 3 days before trial ends nudging users to subscribe
4. **Password reset** — no recovery flow exists yet; users who forget their password are locked out
5. **DeviceDetail multi-tenant** — Force Sync / Restart IME in DeviceDetail currently use `IGraphService` (MockGraphService). In real mode these need to create a `GraphService` for the user's tenant via `GraphServiceFactory` instead.
