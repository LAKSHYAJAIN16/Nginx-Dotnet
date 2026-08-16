namespace NginxDotNet.Core.Adaptors.Transforms;

public class ClaimToHeaderMapper
{
    private readonly Dictionary<string, string> _mappings;

    public ClaimToHeaderMapper(Dictionary<string, string>? mappings = null)
    {
        _mappings = mappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sub"] = "X-User-ID",
            ["role"] = "X-User-Role",
            ["email"] = "X-User-Email",
            ["tenant"] = "X-Tenant-ID"
        };
    }

    public Dictionary<string, string> MapClaimsToHeaders(IDictionary<string, string> claims)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (claimType, claimValue) in claims)
        {
            if (_mappings.TryGetValue(claimType, out var headerName))
            {
                result[headerName] = claimValue;
            }
        }

        return result;
    }
}
