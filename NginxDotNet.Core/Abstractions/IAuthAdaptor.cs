using NginxDotNet.Core.Models;

namespace NginxDotNet.Core.Abstractions;

public interface IAuthAdaptor
{
    string Name { get; }
    Task<AuthResult> AuthenticateAsync(IDictionary<string, string> requestHeaders, string path);
}
