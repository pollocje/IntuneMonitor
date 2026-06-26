using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IntuneMonitor.Pages.Billing;

[Authorize]
public class SuccessModel : PageModel
{
    public void OnGet() { }
}
