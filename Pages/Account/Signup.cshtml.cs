using System.Security.Claims;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IntuneMonitor.Pages.Account;

public class SignupModel : PageModel
{
    private readonly AuthService _auth;

    public SignupModel(AuthService auth) => _auth = auth;

    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;

    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Password != ConfirmPassword)
        {
            Error = "Passwords do not match.";
            return Page();
        }

        if (Password.Length < 8)
        {
            Error = "Password must be at least 8 characters.";
            return Page();
        }

        try
        {
            var user = await _auth.RegisterAsync(Email, Password);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new("TenantId", user.TenantId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

            return Redirect("/onboarding");
        }
        catch (InvalidOperationException ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}
