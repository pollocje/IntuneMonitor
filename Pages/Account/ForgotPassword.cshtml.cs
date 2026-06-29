using IntuneMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IntuneMonitor.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly AuthService _auth;
    private readonly EmailNotificationService _email;
    private readonly IConfiguration _config;

    public bool Sent { get; private set; }

    public ForgotPasswordModel(AuthService auth, EmailNotificationService email, IConfiguration config)
    {
        _auth   = auth;
        _email  = email;
        _config = config;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Page();

        var token = await _auth.GeneratePasswordResetTokenAsync(email);

        if (token is not null)
        {
            var appUrl   = (_config["AppUrl"] ?? "https://localhost:5001").TrimEnd('/');
            var resetUrl = $"{appUrl}/account/reset-password?token={token}";
            await _email.SendPasswordResetAsync(email, resetUrl);
        }

        // Always show the same success message — never reveal if email exists
        Sent = true;
        return Page();
    }
}
