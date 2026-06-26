using IntuneMonitor.Data;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace IntuneMonitor.Pages;

[Authorize]
public class ConnectCallbackModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TenantOnboardingService _onboarding;
    private readonly ILogger<ConnectCallbackModel> _logger;

    public ConnectCallbackModel(AppDbContext db, TenantOnboardingService onboarding, ILogger<ConnectCallbackModel> logger)
    {
        _db         = db;
        _onboarding = onboarding;
        _logger     = logger;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery(Name = "admin_consent")] string? adminConsent,
        [FromQuery(Name = "tenant")]        string? microsoftTenantId,
        [FromQuery(Name = "state")]         string? state,
        [FromQuery(Name = "error")]         string? error)
    {
        if (error is not null || adminConsent?.ToLower() != "true")
            return Redirect("/connect-tenant?error=access_denied");

        if (!Guid.TryParse(state, out var tenantId) || string.IsNullOrEmpty(microsoftTenantId))
            return Redirect("/connect-tenant?error=access_denied");

        // Cross-check state against the logged-in user's tenant to prevent tampering
        var claimValue = User.FindFirstValue("TenantId");
        if (claimValue is null || !Guid.TryParse(claimValue, out var userTenantId) || tenantId != userTenantId)
            return Redirect("/connect-tenant?error=access_denied");

        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant is null)
            return Redirect("/connect-tenant?error=access_denied");

        tenant.MicrosoftTenantId = microsoftTenantId;
        await _db.SaveChangesAsync();

        try
        {
            await _onboarding.SetupTenantAsync(tenant);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Tenant is connected; IME script creation failing is non-fatal (shows "Not Configured" in UI)
            _logger.LogWarning(ex, "Failed to create IME remediation script for tenant {TenantId} — tenant may not have Intune Plan 2", tenantId);
        }

        return Redirect("/connect-tenant?success=1");
    }
}
