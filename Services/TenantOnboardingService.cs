using Azure.Identity;
using IntuneMonitor.Data.Entities;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text;

namespace IntuneMonitor.Services;

public class TenantOnboardingService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<TenantOnboardingService> _logger;

    public TenantOnboardingService(IConfiguration config, ILogger<TenantOnboardingService> logger)
    {
        _clientId     = config["AzureAd:ClientId"]     ?? throw new InvalidOperationException("AzureAd:ClientId not configured.");
        _clientSecret = config["AzureAd:ClientSecret"] ?? throw new InvalidOperationException("AzureAd:ClientSecret not configured.");
        _logger = logger;
    }

    public async Task SetupTenantAsync(Tenant tenant)
    {
        if (string.IsNullOrEmpty(tenant.MicrosoftTenantId))
            throw new InvalidOperationException("Tenant has no MicrosoftTenantId set.");

        // Use our app credentials scoped to the customer's tenant — works because they granted admin consent
        var credential = new ClientSecretCredential(tenant.MicrosoftTenantId, _clientId, _clientSecret);
        var client = new GraphServiceClient(credential);

        var detectionScript = """
            $svc = Get-Service -Name 'IntuneManagementExtension' -ErrorAction SilentlyContinue
            if ($null -eq $svc -or $svc.Status -ne 'Running') { Exit 1 }
            Exit 0
            """;

        var remediationScript = """
            try {
                Restart-Service -Name 'IntuneManagementExtension' -Force -ErrorAction Stop
                Exit 0
            } catch {
                Write-Error $_.Exception.Message
                Exit 1
            }
            """;

        var script = new DeviceHealthScript
        {
            DisplayName              = "IntuneMonitor - Restart IME Service",
            Description              = "Restarts IntuneManagementExtension to fix stuck app installations. Created by IntuneMonitor.",
            Publisher                = "IntuneMonitor",
            RunAsAccount             = RunAsAccountType.System,
            EnforceSignatureCheck    = false,
            RunAs32Bit               = false,
            DetectionScriptContent   = Encoding.UTF8.GetBytes(detectionScript),
            RemediationScriptContent = Encoding.UTF8.GetBytes(remediationScript)
        };

        var created = await client.DeviceManagement.DeviceHealthScripts.PostAsync(script);

        if (created?.Id is not null)
        {
            tenant.RemediationScriptId = created.Id;
            _logger.LogInformation("Created IME remediation script {ScriptId} in tenant {TenantId}",
                created.Id, tenant.MicrosoftTenantId);
        }
    }
}
