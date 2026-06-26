using System.Security.Claims;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IntuneMonitor.Pages.Billing;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly StripeService _stripe;

    public CheckoutModel(StripeService stripe) => _stripe = stripe;

    public async Task<IActionResult> OnGetAsync()
    {
        var tenantId = Guid.Parse(User.FindFirstValue("TenantId")!);
        var email = User.FindFirstValue(ClaimTypes.Email)!;

        var url = await _stripe.CreateCheckoutSessionAsync(tenantId, email);
        return Redirect(url);
    }
}
