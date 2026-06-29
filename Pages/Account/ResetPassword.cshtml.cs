using IntuneMonitor.Data;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IntuneMonitor.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly AuthService _auth;
    private readonly AppDbContext _db;

    public string? Token { get; private set; }
    public string? Error { get; private set; }
    public bool TokenInvalid { get; private set; }
    public bool Success { get; private set; }

    public ResetPasswordModel(AuthService auth, AppDbContext db)
    {
        _auth = auth;
        _db   = db;
    }

    public async Task OnGetAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TokenInvalid = true;
            return;
        }

        var valid = await _db.Users.AnyAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetExpiry > DateTime.UtcNow);

        if (!valid)
            TokenInvalid = true;
        else
            Token = token;
    }

    public async Task<IActionResult> OnPostAsync(string token, string password, string confirm)
    {
        Token = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            TokenInvalid = true;
            return Page();
        }

        if (password != confirm)
        {
            Error = "Passwords do not match.";
            return Page();
        }

        if (password.Length < 8)
        {
            Error = "Password must be at least 8 characters.";
            return Page();
        }

        var ok = await _auth.ResetPasswordAsync(token, password);
        if (!ok)
        {
            TokenInvalid = true;
            return Page();
        }

        Success = true;
        return Page();
    }
}
