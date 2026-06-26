using IntuneMonitor.Data;
using IntuneMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntuneMonitor.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<AppUser?> ValidateAsync(string email, string password)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());

        if (user is null) return null;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<AppUser> RegisterAsync(string email, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email.ToLower()))
            throw new InvalidOperationException("An account with this email already exists.");

        var tenant = new Tenant
        {
            Name = email.Split('@')[0],
            MicrosoftTenantId = string.Empty,
            TrialEndsAt = DateTime.UtcNow.AddDays(14),
            SubscriptionStatus = SubscriptionStatus.Trial
        };

        var user = new AppUser
        {
            TenantId = tenant.Id,
            Email = email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Tenant = tenant
        };

        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }
}
