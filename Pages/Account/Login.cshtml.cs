using System.Security.Claims;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IntuneMonitor.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AuthService _auth;

    public LoginModel(AuthService auth) => _auth = auth;

    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _auth.ValidateAsync(Email, Password);

        if (user is null)
        {
            Error = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("TenantId", user.TenantId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

        return Redirect("/dashboard");
    }
}
