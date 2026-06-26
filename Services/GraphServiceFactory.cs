using Azure.Identity;
using Microsoft.Graph;

namespace IntuneMonitor.Services;

public class GraphServiceFactory
{
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GraphServiceFactory(IConfiguration config)
    {
        _clientId     = config["AzureAd:ClientId"]     ?? "";
        _clientSecret = config["AzureAd:ClientSecret"] ?? "";
    }

    public bool IsConfigured =>
        !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public IGraphService CreateForTenant(string microsoftTenantId)
    {
        var credential = new ClientSecretCredential(microsoftTenantId, _clientId, _clientSecret);
        return new GraphService(new GraphServiceClient(credential));
    }
}
