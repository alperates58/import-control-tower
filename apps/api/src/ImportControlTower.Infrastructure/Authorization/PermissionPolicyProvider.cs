using Microsoft.AspNetCore.Authorization;

namespace ImportControlTower.Infrastructure.Authorization;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder();
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(policyName));
        return Task.FromResult<AuthorizationPolicy?>(policy.Build());
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult<AuthorizationPolicy?>(null);
    }
}
