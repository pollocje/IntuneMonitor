using IntuneMonitor.Data;
using IntuneMonitor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace IntuneMonitor.Services;

public class StripeService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly string _webhookSecret;
    private readonly string _priceId;

    public StripeService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _webhookSecret = config["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe:WebhookSecret not configured.");
        _priceId = config["Stripe:PriceId"] ?? throw new InvalidOperationException("Stripe:PriceId not configured.");

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured.");
    }

    // Creates a Stripe Checkout session and returns the redirect URL
    public async Task<string> CreateCheckoutSessionAsync(Guid tenantId, string email)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        // Create or reuse Stripe customer
        var customerId = tenant.StripeCustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string> { ["tenantId"] = tenantId.ToString() }
            });

            tenant.StripeCustomerId = customer.Id;
            await _db.SaveChangesAsync();
            customerId = customer.Id;
        }

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = _priceId, Quantity = 1 }
            },
            SuccessUrl = $"{_config["AppUrl"]}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_config["AppUrl"]}/dashboard",
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialEnd = tenant.TrialEndsAt.HasValue
                    ? (long?)new DateTimeOffset(tenant.TrialEndsAt.Value).ToUnixTimeSeconds()
                    : null
            }
        });

        return session.Url;
    }

    // Called by the webhook endpoint — updates tenant subscription status based on Stripe events
    public async Task HandleWebhookAsync(string json, string stripeSignature)
    {
        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _webhookSecret);

        switch (stripeEvent.Type)
        {
            case Events.CustomerSubscriptionUpdated:
            case Events.CustomerSubscriptionCreated:
            {
                var subscription = (Subscription)stripeEvent.Data.Object;
                await UpdateSubscriptionStatusAsync(subscription);
                break;
            }

            case Events.CustomerSubscriptionDeleted:
            {
                var subscription = (Subscription)stripeEvent.Data.Object;
                await SetStatusByStripeCustomerAsync(subscription.CustomerId, SubscriptionStatus.Cancelled);
                break;
            }

            case Events.InvoicePaymentFailed:
            {
                var invoice = (Invoice)stripeEvent.Data.Object;
                await SetStatusByStripeCustomerAsync(invoice.CustomerId, SubscriptionStatus.PastDue);
                break;
            }

            case Events.InvoicePaymentSucceeded:
            {
                var invoice = (Invoice)stripeEvent.Data.Object;
                await SetStatusByStripeCustomerAsync(invoice.CustomerId, SubscriptionStatus.Active);
                break;
            }
        }
    }

    private async Task UpdateSubscriptionStatusAsync(Subscription subscription)
    {
        var status = subscription.Status switch
        {
            "active"   => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trial,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            _          => SubscriptionStatus.Cancelled
        };

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.StripeCustomerId == subscription.CustomerId);

        if (tenant is null) return;

        tenant.SubscriptionStatus = status;
        tenant.StripeSubscriptionId = subscription.Id;
        await _db.SaveChangesAsync();
    }

    private async Task SetStatusByStripeCustomerAsync(string customerId, SubscriptionStatus status)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.StripeCustomerId == customerId);

        if (tenant is null) return;

        tenant.SubscriptionStatus = status;
        await _db.SaveChangesAsync();
    }
}
